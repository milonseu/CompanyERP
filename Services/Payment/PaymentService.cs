using CompanyERP.Data;
using CompanyERP.Entities.Asset;
using CompanyERP.Entities.Customer;
using CompanyERP.Entities.Employee;
using CompanyERP.Entities.Expense;
using CompanyERP.Entities.Payment;
using CompanyERP.Entities.Purchase;
using CompanyERP.Entities.Sales;
using CompanyERP.Entities.Supplier;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Payments;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _db;
    private readonly ITransactionPostingService _postingService;

    public PaymentService(ApplicationDbContext db, ITransactionPostingService postingService)
    {
        _db = db;
        _postingService = postingService;
    }

    public async Task<List<Payment>> GetAllAsync(int companyId, PaymentCategory? category = null)
    {
        var query = _db.Payments
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId)
            .Include(p => p.Company)
            .Include(p => p.Branch)
            .Include(p => p.Customer)
            .Include(p => p.Supplier)
            .Include(p => p.PaymentMethod)
            .Include(p => p.CashAccount)
            .Include(p => p.BankAccount)
            .AsQueryable();

        if (category.HasValue)
        {
            query = query.Where(p => p.Category == category.Value);
        }

        return await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.Id)
            .ToListAsync();
    }

    public async Task<Payment?> GetByIdAsync(int id)
    {
        return await _db.Payments
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Include(p => p.Company)
            .Include(p => p.Branch)
            .Include(p => p.Customer)
            .Include(p => p.Supplier)
            .Include(p => p.ExpenseEntry)
            .Include(p => p.SalaryPayment)!.ThenInclude(s => s!.Employee)
            .Include(p => p.AssetRegister)
            .Include(p => p.PaymentMethod)
            .Include(p => p.CashAccount)
            .Include(p => p.BankAccount)
            .FirstOrDefaultAsync();
    }

    public async Task<string> GeneratePaymentNoAsync(int companyId, DateTime paymentDate)
    {
        var count = await _db.Payments
            .CountAsync(p => p.CompanyId == companyId && p.PaymentDate.Date == paymentDate.Date);
        return $"PAY-{paymentDate:yyyyMMdd}-{(count + 1):D3}";
    }

    public async Task<List<SalaryPayment>> GetPendingSalaryCandidatesAsync(int companyId)
    {
        return await _db.SalaryPayments
            .Where(sp => sp.Status != SalaryPaymentStatus.Paid)
            .Where(sp => sp.Employee!.CompanyId == companyId)
            .Include(sp => sp.Employee)
            .OrderBy(sp => sp.Employee!.Name)
            .ToListAsync();
    }

    public async Task<decimal> GetCustomerOutstandingAsync(int customerId)
    {
        var customer = await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer is null)
        {
            return 0;
        }

        var invoiceAmounts = await _db.SalesInvoices
            .AsNoTracking()
            .Where(i => i.CustomerId == customerId)
            .Include(i => i.Lines)
            .ToListAsync();
        var invoiceTotal = invoiceAmounts.Sum(i => i.Lines.Sum(l => l.Quantity * l.UnitPrice));
        var invoicePaid = invoiceAmounts.Sum(i => i.AmountPaid);
        var modulePayments = await _db.Payments
            .Where(p => p.Category == PaymentCategory.Customer && p.CustomerId == customerId)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        return Math.Max(0, customer.OpeningReceivable + invoiceTotal - invoicePaid - modulePayments);
    }

    public async Task<decimal> GetSupplierOutstandingAsync(int supplierId)
    {
        var supplier = await _db.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == supplierId);
        if (supplier is null)
        {
            return 0;
        }

        var invoiceAmounts = await _db.PurchaseInvoices
            .AsNoTracking()
            .Where(i => i.SupplierId == supplierId)
            .Include(i => i.Lines)
            .ToListAsync();
        var invoiceTotal = invoiceAmounts.Sum(i => i.Lines.Sum(l => l.Quantity * l.UnitPrice));
        var modulePayments = await _db.Payments
            .Where(p => p.Category == PaymentCategory.Supplier && p.SupplierId == supplierId)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        return Math.Max(0, supplier.OpeningPayable + invoiceTotal - modulePayments);
    }

    public async Task<decimal> GetExpenseOutstandingAsync(int expenseEntryId)
    {
        var entry = await _db.ExpenseEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == expenseEntryId);
        return entry is null ? 0 : Math.Max(0, entry.Amount - entry.AmountPaid);
    }

    public async Task<decimal> GetAssetOutstandingAsync(int assetRegisterId)
    {
        var asset = await _db.AssetRegisters
            .AsNoTracking()
            .Include(a => a.Acquisition)
            .FirstOrDefaultAsync(a => a.Id == assetRegisterId);
        if (asset is null)
        {
            return 0;
        }

        return Math.Max(0, asset.Cost - (asset.Acquisition?.AmountPaid ?? 0));
    }

    public async Task<(bool Success, string Error)> CreateAsync(Payment payment)
    {
        payment.PaymentNo = string.IsNullOrWhiteSpace(payment.PaymentNo) ? string.Empty : payment.PaymentNo.Trim();
        payment.SourceModule = string.IsNullOrWhiteSpace(payment.SourceModule) ? null : payment.SourceModule.Trim();
        payment.SourceReferenceNo = string.IsNullOrWhiteSpace(payment.SourceReferenceNo) ? null : payment.SourceReferenceNo.Trim();
        payment.ReferenceNo = string.IsNullOrWhiteSpace(payment.ReferenceNo) ? null : payment.ReferenceNo.Trim();
        payment.Note = string.IsNullOrWhiteSpace(payment.Note) ? null : payment.Note.Trim();

        if (string.IsNullOrWhiteSpace(payment.PaymentNo))
        {
            return (false, "Payment number is required.");
        }

        if (payment.Amount <= 0)
        {
            return (false, "Amount must be greater than zero.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == payment.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (!await _db.Branches.AnyAsync(b => b.Id == payment.BranchId && b.CompanyId == payment.CompanyId))
        {
            return (false, "Selected branch does not belong to the company.");
        }

        if (!await _db.PaymentMethods.AnyAsync(m => m.Id == payment.PaymentMethodId && m.CompanyId == payment.CompanyId))
        {
            return (false, "Selected payment method does not belong to the company.");
        }

        if (payment.AccountType == PaymentAccountType.Cash)
        {
            if (!payment.CashAccountId.HasValue)
            {
                return (false, "A cash account is required for cash payments.");
            }

            if (!await _db.CashAccounts.AnyAsync(a => a.Id == payment.CashAccountId.Value && a.CompanyId == payment.CompanyId))
            {
                return (false, "Selected cash account does not belong to the company.");
            }
        }
        else
        {
            if (!payment.BankAccountId.HasValue)
            {
                return (false, "A bank account is required for bank payments.");
            }

            if (!await _db.BankAccounts.AnyAsync(a => a.Id == payment.BankAccountId.Value && a.CompanyId == payment.CompanyId))
            {
                return (false, "Selected bank account does not belong to the company.");
            }
        }

        if (await _db.Payments.AnyAsync(p => p.CompanyId == payment.CompanyId && p.PaymentNo == payment.PaymentNo))
        {
            return (false, $"Payment number '{payment.PaymentNo}' already exists for the company.");
        }

        payment.PaymentDate = payment.PaymentDate == default ? DateTime.Today : payment.PaymentDate;

        switch (payment.Category)
        {
            case PaymentCategory.Customer:
                return await CreateCustomerPaymentAsync(payment);
            case PaymentCategory.Supplier:
                return await CreateSupplierPaymentAsync(payment);
            case PaymentCategory.Expense:
                return await CreateExpensePaymentAsync(payment);
            case PaymentCategory.Salary:
                return await CreateSalaryPaymentAsync(payment);
            case PaymentCategory.Asset:
                return await CreateAssetPaymentAsync(payment);
            default:
                return (false, "Invalid payment category.");
        }
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var payment = await _db.Payments.FindAsync(id);
        if (payment is null)
        {
            return (false, "Payment not found.");
        }

        return (false, "Posted payments are financial records and cannot be deleted.");
    }

    private async Task<(bool Success, string Error)> CreateCustomerPaymentAsync(Payment payment)
    {
        if (!payment.CustomerId.HasValue)
        {
            return (false, "Customer is required for a customer payment.");
        }

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == payment.CustomerId.Value && c.CompanyId == payment.CompanyId);
        if (customer is null)
        {
            return (false, "Selected customer does not belong to the company.");
        }

        if (string.IsNullOrWhiteSpace(payment.SourceReferenceNo))
        {
            return (false, "Source reference number is required for customer payments.");
        }

        var outstanding = await GetCustomerOutstandingAsync(customer.Id);
        if (payment.Amount > outstanding)
        {
            return (false, $"Payment amount exceeds the customer's outstanding receivable of {outstanding:N2}.");
        }

        payment.SourceModule = "Sales Invoice";
        var post = await _postingService.PostCustomerPaymentAsync(payment);
        if (!post.Success)
        {
            return (false, post.Error);
        }

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    private async Task<(bool Success, string Error)> CreateSupplierPaymentAsync(Payment payment)
    {
        if (!payment.SupplierId.HasValue)
        {
            return (false, "Supplier is required for a supplier payment.");
        }

        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == payment.SupplierId.Value && s.CompanyId == payment.CompanyId);
        if (supplier is null)
        {
            return (false, "Selected supplier does not belong to the company.");
        }

        if (string.IsNullOrWhiteSpace(payment.SourceReferenceNo))
        {
            return (false, "Source reference number is required for supplier payments.");
        }

        var outstanding = await GetSupplierOutstandingAsync(supplier.Id);
        if (payment.Amount > outstanding)
        {
            return (false, $"Payment amount exceeds the supplier's outstanding payable of {outstanding:N2}.");
        }

        payment.SourceModule = "Purchase Invoice";
        var post = await _postingService.PostSupplierPaymentAsync(payment);
        if (!post.Success)
        {
            return (false, post.Error);
        }

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    private async Task<(bool Success, string Error)> CreateExpensePaymentAsync(Payment payment)
    {
        if (!payment.ExpenseEntryId.HasValue)
        {
            return (false, "Expense entry is required for an expense payment.");
        }

        var entry = await _db.ExpenseEntries
            .FirstOrDefaultAsync(e => e.Id == payment.ExpenseEntryId.Value && e.CompanyId == payment.CompanyId);
        if (entry is null)
        {
            return (false, "Selected expense entry does not belong to the company.");
        }

        var outstanding = entry.Amount - entry.AmountPaid;
        if (outstanding <= 0)
        {
            return (false, "The selected expense entry has no outstanding balance.");
        }

        if (payment.Amount > outstanding)
        {
            return (false, $"Payment amount exceeds the expense entry's outstanding balance of {outstanding:N2}.");
        }

        payment.SourceModule = "Expense Entry";
        payment.SourceReferenceNo = entry.ExpenseNo;
        entry.AmountPaid += payment.Amount;
        if (entry.PaymentType == ExpensePaymentType.Payable)
        {
            entry.PaymentType = payment.AccountType == PaymentAccountType.Cash ? ExpensePaymentType.Cash : ExpensePaymentType.Bank;
        }

        _db.Payments.Add(payment);
        var post = await _postingService.PostExpensePaymentAsync(payment, entry);
        if (!post.Success)
        {
            return (false, post.Error);
        }

        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    private async Task<(bool Success, string Error)> CreateSalaryPaymentAsync(Payment payment)
    {
        if (!payment.SalaryPaymentId.HasValue)
        {
            return (false, "Salary payment is required for a salary payment.");
        }

        var salary = await _db.SalaryPayments
            .Include(sp => sp.Employee)
            .FirstOrDefaultAsync(sp => sp.Id == payment.SalaryPaymentId.Value && sp.Employee!.CompanyId == payment.CompanyId);
        if (salary is null)
        {
            return (false, "Selected salary payment does not belong to the company.");
        }

        if (salary.Status == SalaryPaymentStatus.Paid)
        {
            return (false, "The selected salary payment is already paid.");
        }

        if (payment.Amount > salary.Amount)
        {
            return (false, $"Payment amount exceeds the salary amount of {salary.Amount:N2}.");
        }

        payment.SourceModule = "Salary Payment";
        payment.SourceReferenceNo = salary.ReferenceNo ?? salary.ForMonth.ToString("yyyy-MM");
        salary.Status = SalaryPaymentStatus.Paid;
        salary.PaymentMode = payment.AccountType == PaymentAccountType.Cash ? PaymentMode.Cash : PaymentMode.Bank;
        salary.ReferenceNo = payment.ReferenceNo;
        salary.PaymentDate = payment.PaymentDate;

        _db.Payments.Add(payment);
        var post = await _postingService.PostSalaryPaymentAsync(payment);
        if (!post.Success)
        {
            return (false, post.Error);
        }

        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    private async Task<(bool Success, string Error)> CreateAssetPaymentAsync(Payment payment)
    {
        if (!payment.AssetRegisterId.HasValue)
        {
            return (false, "Asset is required for an asset payment.");
        }

        var asset = await _db.AssetRegisters
            .Include(a => a.Acquisition)
            .FirstOrDefaultAsync(a => a.Id == payment.AssetRegisterId.Value && a.CompanyId == payment.CompanyId);
        if (asset is null)
        {
            return (false, "Selected asset does not belong to the company.");
        }

        if (string.IsNullOrWhiteSpace(payment.SourceReferenceNo))
        {
            return (false, "Source reference number is required for asset payments.");
        }

        var paid = asset.Acquisition?.AmountPaid ?? 0;
        var outstanding = asset.Cost - paid;
        if (outstanding <= 0)
        {
            return (false, "The selected asset has no outstanding payable balance.");
        }

        if (payment.Amount > outstanding)
        {
            return (false, $"Payment amount exceeds the asset's outstanding payable of {outstanding:N2}.");
        }

        payment.SourceModule = "Asset Acquisition";
        if (asset.Acquisition is not null)
        {
            asset.Acquisition.AmountPaid += payment.Amount;
        }

        _db.Payments.Add(payment);
        var post = await _postingService.PostAssetPaymentAsync(payment);
        if (!post.Success)
        {
            return (false, post.Error);
        }

        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}