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
    public const string OpeningReference = "Opening";

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

    /// <summary>
    /// Everything that moves a customer receivable: what was invoiced, returned, paid and refunded.
    /// </summary>
    private async Task<(decimal Opening, decimal Invoiced, decimal Returned, decimal Paid, decimal Refunded)> GetCustomerTotalsAsync(
        int customerId, bool customerExists)
    {
        if (!customerExists)
        {
            return (0m, 0m, 0m, 0m, 0m);
        }

        var customer = await _db.Customers
            .AsNoTracking()
            .FirstAsync(c => c.Id == customerId);

        var invoices = await _db.SalesInvoices
            .AsNoTracking()
            .Where(i => i.CustomerId == customerId)
            .Include(i => i.Lines)
            .ToListAsync();

        // A posted sales return reduces what the customer owes (it credits receivable), so it has to
        // come off the balance here as well as in the ledger.
        var returns = await _db.SalesReturns
            .AsNoTracking()
            .Where(r => r.CustomerId == customerId && r.Status == SalesReturnStatus.Posted)
            .Include(r => r.Lines)
            .ToListAsync();

        var paid = await _db.Payments
            .Where(p => p.Category == PaymentCategory.Customer && p.CustomerId == customerId)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;
        var refunded = await _db.Payments
            .Where(p => p.Category == PaymentCategory.CustomerRefund && p.CustomerId == customerId)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        return (
            customer.OpeningReceivable,
            Math.Round(invoices.Sum(i => i.Lines.Sum(l => l.Quantity * l.UnitPrice)), 2),
            Math.Round(returns.Sum(r => r.Lines.Sum(l => l.Quantity * l.UnitPrice)), 2),
            Math.Round(invoices.Sum(i => i.AmountPaid) + paid, 2),
            Math.Round(refunded, 2));
    }

    public async Task<decimal> GetCustomerOutstandingAsync(int customerId)
    {
        var exists = await _db.Customers.AsNoTracking().AnyAsync(c => c.Id == customerId);
        if (!exists)
        {
            return 0;
        }

        var t = await GetCustomerTotalsAsync(customerId, true);
        return Math.Max(0, Math.Round(t.Opening + t.Invoiced - t.Returned - t.Paid + t.Refunded, 2));
    }

    /// <summary>
    /// Signed receivable balance. A sales return on an already paid invoice leaves the customer in
    /// credit, and that credit is what a refund may be paid against, so it must not be clamped away.
    /// </summary>
    public async Task<decimal> GetCustomerBalanceAsync(int customerId)
    {
        var exists = await _db.Customers.AsNoTracking().AnyAsync(c => c.Id == customerId);
        if (!exists)
        {
            return 0;
        }

        var t = await GetCustomerTotalsAsync(customerId, true);
        return Math.Round(t.Opening + t.Invoiced - t.Returned - t.Paid + t.Refunded, 2);
    }

    /// <summary>Credit available to pay back to the customer, i.e. the negative side of the balance.</summary>
    public async Task<decimal> GetCustomerRefundableAsync(int customerId)
    {
        var balance = await GetCustomerBalanceAsync(customerId);
        return balance < 0 ? Math.Round(-balance, 2) : 0m;
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
            case PaymentCategory.CustomerRefund:
                return await CreateCustomerRefundAsync(payment);
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

    private static bool IsOpeningReference(string? sourceReferenceNo) =>
        string.Equals(sourceReferenceNo?.Trim(), OpeningReference, StringComparison.OrdinalIgnoreCase);

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
        if (payment.Amount > outstanding && !IsOpeningReference(payment.SourceReferenceNo))
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

    /// <summary>
    /// Pays money back to a customer who is in credit, which is what a sales return against an
    /// already paid invoice leaves behind. Posts the reverse of a receipt: the receivable falls
    /// because the customer's claim on us falls, and cash/bank leaves the business.
    /// </summary>
    private async Task<(bool Success, string Error)> CreateCustomerRefundAsync(Payment payment)
    {
        if (!payment.CustomerId.HasValue)
        {
            return (false, "Customer is required for a refund.");
        }

        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == payment.CustomerId.Value && c.CompanyId == payment.CompanyId);
        if (customer is null)
        {
            return (false, "Selected customer does not belong to the company.");
        }

        var refundable = await GetCustomerRefundableAsync(customer.Id);
        if (refundable <= 0)
        {
            return (false, $"{customer.Name} has no credit balance to refund.");
        }

        if (payment.Amount > refundable)
        {
            return (false, $"Refund amount exceeds the available credit of {refundable:N2}. " +
                "The rest stays as a credit on the customer account.");
        }

        if (string.IsNullOrWhiteSpace(payment.SourceReferenceNo))
        {
            return (false, "A return or credit note number is required for a refund.");
        }

        payment.SourceModule = "Customer Refund";

        var post = await _postingService.PostCustomerRefundAsync(payment);
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
        if (payment.Amount > outstanding && !IsOpeningReference(payment.SourceReferenceNo))
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