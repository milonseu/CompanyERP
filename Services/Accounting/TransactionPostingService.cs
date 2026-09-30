using CompanyERP.Data;
using CompanyERP.Entities.Accounting;
using CompanyERP.Entities.Asset;
using CompanyERP.Entities.Company;
using CompanyERP.Entities.Expense;
using CompanyERP.Entities.Payment;
using CompanyERP.Entities.Sales;
using CompanyERP.Entities.Inventory;
using CompanyERP.Entities.Purchase;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using EmployeeSalary = CompanyERP.Entities.Employee.SalaryPayment;
using PaymentAccountType = CompanyERP.Entities.Payment.PaymentAccountType;
using PaymentMode = CompanyERP.Entities.Employee.PaymentMode;

namespace CompanyERP.Services.Accounting;

/// <summary>
/// Real-time accounting posting. Methods stage balanced journal entries against the
/// shared ApplicationDbContext and never call SaveChangesAsync themselves; the calling
/// module service commits the transaction so the entry is created atomically with the
/// source record.
/// </summary>
public class TransactionPostingService : ITransactionPostingService
{
    private readonly ApplicationDbContext _db;

    public TransactionPostingService(ApplicationDbContext db)
    {
        _db = db;
    }

    private const string Cash = "1000";
    private const string Bank = "1100";
    private const string AccountsReceivable = "1200";
    private const string Inventory = "1300";
    private const string FixedAssets = "1400";
    private const string AccumulatedDepreciation = "1410";
    private const string AccountsPayable = "2000";
    private const string ExpensePayable = "2100";
    private const string SalaryPayable = "2200";
    private const string AssetPayable = "2300";
    private const string Equity = "3000";
    private const string ProductRevenue = "4000";
    private const string SoftwareRevenue = "4100";
    private const string ServiceRevenue = "4200";
    private const string CostOfGoodsSold = "5000";
    private const string InventoryWriteOff = "5010";
    private const string SalaryExpense = "5100";
    private const string DepreciationExpense = "5200";
    private const string OperatingExpense = "5300";
    private const string GainOnDisposal = "5400";
    private const string LossOnDisposal = "5410";
    private const string PurchasePriceVariance = "5500";
    private const string InventoryGain = "4310";

    // Default 4-layer chart of accounts seeded per company so every posting always resolves.
    // Codes stay on ROWS; parents are named explicitly. Parent-first ordering matters only
    // for readability here; EnsureDefaultsAsync resolves by code so any order is safe.
    // (code, name, type, normalBalance, parentCode)
    private static readonly List<(string Code, string Name, AccountType Type, AccountNormalBalance Balance, string? ParentCode)> DefaultAccountTree =
    [
        // Level 1 - Class
        ("1", "Assets", AccountType.Asset, AccountNormalBalance.Debit, null),
        ("2", "Liabilities", AccountType.Liability, AccountNormalBalance.Credit, null),
        ("3", "Equity", AccountType.Equity, AccountNormalBalance.Credit, null),
        ("4", "Revenue", AccountType.Revenue, AccountNormalBalance.Credit, null),
        ("5", "Expenses", AccountType.Expense, AccountNormalBalance.Debit, null),

        // Level 2 - Group
        ("10", "Cash & Bank", AccountType.Asset, AccountNormalBalance.Debit, "1"),
        ("11", "Accounts Receivable", AccountType.Asset, AccountNormalBalance.Debit, "1"),
        ("12", "Inventory", AccountType.Asset, AccountNormalBalance.Debit, "1"),
        ("13", "Fixed Assets", AccountType.Asset, AccountNormalBalance.Debit, "1"),
        ("20", "Accounts Payable", AccountType.Liability, AccountNormalBalance.Credit, "2"),
        ("21", "Other Payables", AccountType.Liability, AccountNormalBalance.Credit, "2"),
        ("30", "Opening Balance Equity", AccountType.Equity, AccountNormalBalance.Credit, "3"),
        ("40", "Product Sales", AccountType.Revenue, AccountNormalBalance.Credit, "4"),
        ("41", "Software Sales", AccountType.Revenue, AccountNormalBalance.Credit, "4"),
        ("42", "Service Sales", AccountType.Revenue, AccountNormalBalance.Credit, "4"),
        ("43", "Other Income", AccountType.Revenue, AccountNormalBalance.Credit, "4"),
        ("50", "Cost of Goods Sold", AccountType.Expense, AccountNormalBalance.Debit, "5"),
        ("51", "Operating Expenses", AccountType.Expense, AccountNormalBalance.Debit, "5"),
        ("52", "Employee Costs", AccountType.Expense, AccountNormalBalance.Debit, "5"),
        ("53", "Depreciation", AccountType.Expense, AccountNormalBalance.Debit, "5"),
        ("54", "Other Expenses", AccountType.Expense, AccountNormalBalance.Debit, "5"),

        // Level 3 - Sub-Group
        ("100", "Cash Accounts", AccountType.Asset, AccountNormalBalance.Debit, "10"),
        ("110", "Bank Accounts", AccountType.Asset, AccountNormalBalance.Debit, "10"),
        ("120", "Trade Receivables", AccountType.Asset, AccountNormalBalance.Debit, "11"),
        ("130", "Inventory", AccountType.Asset, AccountNormalBalance.Debit, "12"),
        ("140", "Fixed Assets", AccountType.Asset, AccountNormalBalance.Debit, "13"),
        ("141", "Accumulated Depreciation", AccountType.Asset, AccountNormalBalance.Credit, "13"),
        ("200", "Trade Payables", AccountType.Liability, AccountNormalBalance.Credit, "20"),
        ("210", "Accrued Expenses", AccountType.Liability, AccountNormalBalance.Credit, "20"),
        ("220", "Payroll Liabilities", AccountType.Liability, AccountNormalBalance.Credit, "20"),
        ("230", "Asset Financing", AccountType.Liability, AccountNormalBalance.Credit, "21"),
        ("300", "Opening Equity", AccountType.Equity, AccountNormalBalance.Credit, "30"),
        ("400", "Product Sales", AccountType.Revenue, AccountNormalBalance.Credit, "40"),
        ("410", "Software Sales", AccountType.Revenue, AccountNormalBalance.Credit, "41"),
        ("420", "Service Sales", AccountType.Revenue, AccountNormalBalance.Credit, "42"),
        ("430", "Other Income", AccountType.Revenue, AccountNormalBalance.Credit, "43"),
        ("500", "Cost of Goods Sold", AccountType.Expense, AccountNormalBalance.Debit, "50"),
        ("510", "Employee Costs", AccountType.Expense, AccountNormalBalance.Debit, "52"),
        ("520", "Depreciation", AccountType.Expense, AccountNormalBalance.Debit, "53"),
        ("530", "General & Administrative", AccountType.Expense, AccountNormalBalance.Debit, "51"),
        ("540", "Other Expenses", AccountType.Expense, AccountNormalBalance.Debit, "54"),

        // Level 4 - Leaf (postable accounts keep the module posting codes)
        (Cash, "Cash", AccountType.Asset, AccountNormalBalance.Debit, "100"),
        (Bank, "Bank", AccountType.Asset, AccountNormalBalance.Debit, "110"),
        (AccountsReceivable, "Accounts Receivable", AccountType.Asset, AccountNormalBalance.Debit, "120"),
        (Inventory, "Inventory", AccountType.Asset, AccountNormalBalance.Debit, "130"),
        (FixedAssets, "Fixed Assets", AccountType.Asset, AccountNormalBalance.Debit, "140"),
        (AccumulatedDepreciation, "Accumulated Depreciation", AccountType.Asset, AccountNormalBalance.Credit, "141"),
        (AccountsPayable, "Accounts Payable", AccountType.Liability, AccountNormalBalance.Credit, "200"),
        (ExpensePayable, "Expense Payable", AccountType.Liability, AccountNormalBalance.Credit, "210"),
        (SalaryPayable, "Salary Payable", AccountType.Liability, AccountNormalBalance.Credit, "220"),
        (AssetPayable, "Asset Payable", AccountType.Liability, AccountNormalBalance.Credit, "230"),
        (Equity, "Opening Balance Equity", AccountType.Equity, AccountNormalBalance.Credit, "300"),
        (ProductRevenue, "Product Sales Revenue", AccountType.Revenue, AccountNormalBalance.Credit, "400"),
        (SoftwareRevenue, "Software Revenue", AccountType.Revenue, AccountNormalBalance.Credit, "410"),
        (ServiceRevenue, "Service Revenue", AccountType.Revenue, AccountNormalBalance.Credit, "420"),
        (CostOfGoodsSold, "Cost of Goods Sold", AccountType.Expense, AccountNormalBalance.Debit, "500"),
        (InventoryWriteOff, "Inventory Write-off", AccountType.Expense, AccountNormalBalance.Debit, "500"),
        (SalaryExpense, "Salary Expense", AccountType.Expense, AccountNormalBalance.Debit, "510"),
        (DepreciationExpense, "Depreciation Expense", AccountType.Expense, AccountNormalBalance.Debit, "520"),
        (OperatingExpense, "Operating Expenses", AccountType.Expense, AccountNormalBalance.Debit, "530"),
        (GainOnDisposal, "Gain on Asset Disposal", AccountType.Revenue, AccountNormalBalance.Credit, "430"),
        (LossOnDisposal, "Loss on Asset Disposal", AccountType.Expense, AccountNormalBalance.Debit, "540"),
        (PurchasePriceVariance, "Purchase Price Variance", AccountType.Expense, AccountNormalBalance.Debit, "540"),
        (InventoryGain, "Inventory Gain", AccountType.Revenue, AccountNormalBalance.Credit, "430")
    ];

    public async Task<(bool Success, string Error)> EnsureDefaultsAsync(int companyId)
    {
        if (!await _db.Companies.AnyAsync(c => c.Id == companyId))
        {
            return (false, "Company does not exist.");
        }

        var existing = await _db.ChartOfAccounts
            .Where(a => a.CompanyId == companyId)
            .ToListAsync();
        var byCode = existing.ToDictionary(a => a.AccountCode, a => a);

        foreach (var (code, name, type, normal, parentCode) in DefaultAccountTree)
        {
            if (byCode.TryGetValue(code, out var account))
            {
                account.AccountName = name;
                account.AccountType = type;
                account.NormalBalance = normal;
                account.IsActive = true;
                if (string.IsNullOrWhiteSpace(account.Description))
                {
                    account.Description = null;
                }
            }
            else
            {
                account = new ChartOfAccount
                {
                    CompanyId = companyId,
                    AccountCode = code,
                    AccountName = name,
                    AccountType = type,
                    NormalBalance = normal,
                    IsActive = true
                };
                _db.ChartOfAccounts.Add(account);
                byCode.Add(code, account);
            }
        }

        // Link parents via the navigation so relationship fixup works for newly added rows.
        foreach (var (code, _, _, _, parentCode) in DefaultAccountTree)
        {
            if (parentCode is null || !byCode.TryGetValue(code, out var account))
            {
                continue;
            }

            if (byCode.TryGetValue(parentCode, out var parent))
            {
                account.Parent = parent;
            }
        }

        // Recompute postable/leaf flags for every company account from the full child map
        // (covers seeded hierarchy plus UI-created parents). Unsaved parents share Id=0,
        // so reference-keyed links are used for them; persisted parents use their id.
        var allAccounts = existing.Concat(_db.ChartOfAccounts.Local)
            .DistinctBy(a => new { a.Id, a.AccountCode })
            .ToList();

        var childrenByRef = new Dictionary<ChartOfAccount, List<ChartOfAccount>>();
        var childrenById = new Dictionary<int, List<ChartOfAccount>>();
        foreach (var account in allAccounts)
        {
            var parent = account.Parent;
            if (parent is not null)
            {
                if (!childrenByRef.TryGetValue(parent, out var children))
                {
                    children = [];
                    childrenByRef[parent] = children;
                }

                children.Add(account);
            }
            else if (account.ParentId.HasValue && account.ParentId.Value != 0)
            {
                if (!childrenById.TryGetValue(account.ParentId.Value, out var children))
                {
                    children = [];
                    childrenById[account.ParentId.Value] = children;
                }

                children.Add(account);
            }
        }

        foreach (var account in allAccounts)
        {
            var isParent = (account.Id != 0 && childrenById.ContainsKey(account.Id)) || childrenByRef.ContainsKey(account);
            account.IsPostable = !isParent;
            account.IsLeaf = !isParent;
        }

        // Commit here so a standalone seed (startup, or the seeding utility) persists the tree
        // even when no posting follows. The posting paths that call this method always call
        // SaveChangesAsync themselves, so this extra save only adds the rows a little earlier.
        await _db.SaveChangesAsync();

        return (true, string.Empty);
    }

    // ---- Module integrated posting methods (stage only; caller saves) ----

    public Task<(bool Success, string Error)> PostCustomerOpeningAsync(Entities.Customer.Customer customer, decimal openingReceivable)
    {
        if (openingReceivable <= 0)
        {
            return Task.FromResult((true, string.Empty));
        }

        return PostAsync(customer.CompanyId, DateTime.Today, "Customer Opening Balance", customer.CustomerCode,
            $"Opening receivable for customer {customer.Name}",
            [(AccountsReceivable, openingReceivable, 0, null), (Equity, 0, openingReceivable, null)]);
    }

    public Task<(bool Success, string Error)> PostSupplierOpeningAsync(Entities.Supplier.Supplier supplier, decimal openingPayable)
    {
        if (openingPayable <= 0)
        {
            return Task.FromResult((true, string.Empty));
        }

        return PostAsync(supplier.CompanyId, DateTime.Today, "Supplier Opening Balance", supplier.SupplierCode,
            $"Opening payable for supplier {supplier.Name}",
            [(Equity, openingPayable, 0, null), (AccountsPayable, 0, openingPayable, null)]);
    }

    public async Task<(bool Success, string Error)> PostSalesInvoiceAsync(SalesInvoice invoice, IReadOnlyDictionary<int, decimal> cogsByProduct)
    {
        var companyId = invoice.CompanyId;
        var total = invoice.Lines.Sum(l => l.Quantity * l.UnitPrice);
        var paidPortion = Math.Round(Math.Min(invoice.AmountPaid, total), 2);
        var receivable = Math.Round(total - paidPortion, 2);

        var lines = new List<(string Code, decimal Debit, decimal Credit, string? Note)>
        {
            (invoice.PaymentType == SalesPaymentType.Bank ? Bank : Cash, paidPortion, 0, "Payment received on invoice")
        };

        var productTotal = invoice.Lines.Where(l => l.ItemType == SalesItemType.Product).Sum(l => l.Quantity * l.UnitPrice);
        var softwareTotal = invoice.Lines.Where(l => l.ItemType == SalesItemType.Software).Sum(l => l.Quantity * l.UnitPrice);
        var serviceTotal = invoice.Lines.Where(l => l.ItemType == SalesItemType.Service).Sum(l => l.Quantity * l.UnitPrice);

        if (productTotal > 0)
        {
            lines.Add((ProductRevenue, 0, productTotal, "Product sales"));
        }

        if (softwareTotal > 0)
        {
            lines.Add((SoftwareRevenue, 0, softwareTotal, "Software sales"));
        }

        if (serviceTotal > 0)
        {
            lines.Add((ServiceRevenue, 0, serviceTotal, "Service sales"));
        }

        if (receivable > 0)
        {
            lines.Add((AccountsReceivable, receivable, 0, "Unpaid invoice balance"));
        }

        // The dictionaries already hold a per-product total, so sum the values once. Adding them
        // per line would count an invoice that repeats a product more than once.
        var cogs = Math.Round(cogsByProduct.Values.Sum(), 2);
        if (cogs > 0)
        {
            lines.Add((CostOfGoodsSold, cogs, 0, "Cost of goods sold"));
            lines.Add((Inventory, 0, cogs, "Inventory reduction"));
        }

        return await PostAsync(companyId, invoice.InvoiceDate, "Sales Invoice", invoice.InvoiceNo,
            $"Sales invoice {invoice.InvoiceNo}", lines, invoice.BranchId);
    }

    public async Task<(bool Success, string Error)> PostSalesReturnAsync(SalesReturn salesReturn, SalesInvoice invoice, IReadOnlyDictionary<int, decimal> reversalByProduct)
    {
        var lines = new List<(string Code, decimal Debit, decimal Credit, string? Note)>();

        var productTotal = salesReturn.Lines.Where(l => l.ItemType == SalesItemType.Product).Sum(l => l.Quantity * l.UnitPrice);
        var softwareTotal = salesReturn.Lines.Where(l => l.ItemType == SalesItemType.Software).Sum(l => l.Quantity * l.UnitPrice);
        var serviceTotal = salesReturn.Lines.Where(l => l.ItemType == SalesItemType.Service).Sum(l => l.Quantity * l.UnitPrice);

        if (productTotal > 0)
        {
            lines.Add((ProductRevenue, productTotal, 0, "Sales return - products"));
        }

        if (softwareTotal > 0)
        {
            lines.Add((SoftwareRevenue, softwareTotal, 0, "Sales return - software"));
        }

        if (serviceTotal > 0)
        {
            lines.Add((ServiceRevenue, serviceTotal, 0, "Sales return - services"));
        }

        var total = Math.Round(productTotal + softwareTotal + serviceTotal, 2);
        if (total > 0)
        {
            lines.Add((AccountsReceivable, 0, total, "Customer credit for returned goods"));
        }

        var reversal = Math.Round(reversalByProduct.Values.Sum(), 2);
        if (reversal > 0)
        {
            lines.Add((Inventory, reversal, 0, "Inventory restored"));
            lines.Add((CostOfGoodsSold, 0, reversal, "COGS reversal"));
        }

        return await PostAsync(salesReturn.CompanyId, salesReturn.ReturnDate, "Sales Return", salesReturn.ReturnNo,
            $"Sales return {salesReturn.ReturnNo}", lines, salesReturn.BranchId);
    }

    public async Task<(bool Success, string Error)> PostPurchaseInvoiceAsync(PurchaseInvoice invoice, IReadOnlyDictionary<int, decimal> receivedCostByProduct)
    {
        // Accounts Payable carries the invoiced amount we owe the supplier. Inventory carries the
        // cost the goods actually came in at, so the stock subledger and account 1300 stay equal;
        // the difference between the two is a purchase price variance.
        var supplierPayable = Math.Round(invoice.Lines.Sum(l => l.Quantity * l.UnitPrice), 2);
        var inventoryValue = Math.Round(invoice.Lines.Sum(l =>
            l.Quantity * (receivedCostByProduct.TryGetValue(l.ProductId, out var received) ? received : l.UnitPrice)), 2);
        var variance = Math.Round(supplierPayable - inventoryValue, 2);

        var lines = new List<(string Code, decimal Debit, decimal Credit, string? Note)>
        {
            (Inventory, inventoryValue, 0, "Purchased goods received"),
            (AccountsPayable, 0, supplierPayable, "Supplier payable")
        };

        if (variance > 0)
        {
            lines.Add((PurchasePriceVariance, variance, 0, "Invoiced cost above received cost"));
        }
        else if (variance < 0)
        {
            lines.Add((PurchasePriceVariance, 0, Math.Abs(variance), "Invoiced cost below received cost"));
        }

        return await PostAsync(invoice.CompanyId, invoice.InvoiceDate, "Purchase Invoice", invoice.InvoiceNo,
            $"Purchase invoice {invoice.InvoiceNo}", lines, invoice.BranchId);
    }

    public async Task<(bool Success, string Error)> PostPurchaseReturnAsync(PurchaseReturn purchaseReturn, decimal relievedValue)
    {
        // The supplier credits the invoiced amount, while our books relieve stock at the moving
        // average we actually carry. Any gap between the two is a purchase price variance.
        var supplierCredit = Math.Round(purchaseReturn.Lines.Sum(l => l.Quantity * l.UnitCost), 2);
        var inventoryRelief = Math.Round(relievedValue, 2);
        var variance = Math.Round(supplierCredit - inventoryRelief, 2);

        var lines = new List<(string Code, decimal Debit, decimal Credit, string? Note)>
        {
            (AccountsPayable, supplierCredit, 0, "Supplier credit for returned goods"),
            (Inventory, 0, inventoryRelief, "Returned goods out at average cost")
        };

        if (variance > 0)
        {
            lines.Add((PurchasePriceVariance, variance, 0, "Returned cost below carrying value"));
        }
        else if (variance < 0)
        {
            lines.Add((PurchasePriceVariance, 0, Math.Abs(variance), "Returned cost above carrying value"));
        }

        return await PostAsync(purchaseReturn.CompanyId, purchaseReturn.ReturnDate, "Purchase Return", purchaseReturn.ReturnNo,
            $"Purchase return {purchaseReturn.ReturnNo}", lines, purchaseReturn.BranchId);
    }

    /// <summary>
    /// Opening stock brought onto the books: Debit Inventory, Credit Opening Balance Equity, so the
    /// balance sheet carries the stock value and the capital that funded it.
    /// </summary>
    public Task<(bool Success, string Error)> PostStockOpeningAsync(int companyId, DateTime entryDate, string referenceNo, decimal value, string description, string? note)
    {
        var amount = Math.Round(value, 2);
        if (amount <= 0)
        {
            return Task.FromResult((true, string.Empty));
        }

        return PostAsync(companyId, entryDate, "Stock Opening", referenceNo, description,
            [(Inventory, amount, 0, "Opening stock value"), (Equity, 0, amount, "Opening balance equity")]);
    }

    /// <summary>
    /// Physical count difference. A shortfall is written off to expense; a surplus is recognised as
    /// inventory gain. Signed: negative value = write-off, positive value = gain.
    /// </summary>
    public Task<(bool Success, string Error)> PostStockAdjustmentAsync(int companyId, DateTime entryDate, string referenceNo, decimal valueEffect, string description, string? note)
    {
        var amount = Math.Round(valueEffect, 2);
        if (amount == 0)
        {
            return Task.FromResult((true, string.Empty));
        }

        var lines = amount > 0
            ? new List<(string Code, decimal Debit, decimal Credit, string? Note)>
            {
                (Inventory, amount, 0, "Stock surplus counted"),
                (InventoryGain, 0, amount, "Inventory gain")
            }
            : new List<(string Code, decimal Debit, decimal Credit, string? Note)>
            {
                (InventoryWriteOff, Math.Abs(amount), 0, "Stock shortage written off"),
                (Inventory, 0, Math.Abs(amount), "Stock shortage relieved")
            };

        return PostAsync(companyId, entryDate, "Stock Adjustment", referenceNo, description, lines);
    }

    public async Task<(bool Success, string Error)> PostCustomerPaymentAsync(Payment payment)
    {
        var account = payment.AccountType == PaymentAccountType.Cash ? Cash : Bank;
        return await PostAsync(payment.CompanyId, payment.PaymentDate, "Customer Payment", payment.PaymentNo,
            $"Customer payment {payment.PaymentNo}",
            [(account, payment.Amount, 0, "Cash received"), (AccountsReceivable, 0, payment.Amount, "Receivable settled")], payment.BranchId);
    }

    public Task<(bool Success, string Error)> PostCustomerRefundAsync(Payment payment)
    {
        // Reverse of a customer receipt: the receivable is reduced because the customer is paying
        // back credit, and cash or bank leaves the business.
        var account = payment.AccountType == PaymentAccountType.Cash ? Cash : Bank;
        return PostAsync(payment.CompanyId, payment.PaymentDate, "Customer Refund", payment.PaymentNo,
            $"Customer refund {payment.PaymentNo}",
            [(AccountsReceivable, payment.Amount, 0, "Customer credit settled"),
             (account, 0, payment.Amount, "Cash refunded")], payment.BranchId);
    }

    public async Task<(bool Success, string Error)> PostSupplierPaymentAsync(Payment payment)
    {
        var account = payment.AccountType == PaymentAccountType.Cash ? Cash : Bank;
        return await PostAsync(payment.CompanyId, payment.PaymentDate, "Supplier Payment", payment.PaymentNo,
            $"Supplier payment {payment.PaymentNo}",
            [(AccountsPayable, payment.Amount, 0, "Payable settled"), (account, 0, payment.Amount, "Cash paid")], payment.BranchId);
    }

    public async Task<(bool Success, string Error)> PostExpenseEntryAsync(ExpenseEntry entry)
    {
        var lines = new List<(string Code, decimal Debit, decimal Credit, string? Note)>
        {
            (OperatingExpense, entry.Amount, 0, entry.Description)
        };

        var paid = Math.Round(Math.Min(entry.AmountPaid, entry.Amount), 2);
        if (paid > 0)
        {
            var account = entry.PaymentType == ExpensePaymentType.Bank ? Bank : Cash;
            lines.Add((account, 0, paid, "Expense paid"));
        }

        var payable = Math.Round(entry.Amount - paid, 2);
        if (payable > 0)
        {
            lines.Add((ExpensePayable, 0, payable, "Expense payable"));
        }

        return await PostAsync(entry.CompanyId, entry.ExpenseDate, "Expense Entry", entry.ExpenseNo,
            $"Expense entry {entry.ExpenseNo}", lines, entry.BranchId);
    }

    public async Task<(bool Success, string Error)> PostExpensePaymentAsync(Payment payment, ExpenseEntry entry)
    {
        var account = payment.AccountType == PaymentAccountType.Cash ? Cash : Bank;
        return await PostAsync(payment.CompanyId, payment.PaymentDate, "Expense Payment", payment.PaymentNo,
            $"Expense payment {payment.PaymentNo} for {entry.ExpenseNo}",
            [(ExpensePayable, payment.Amount, 0, "Expense payable settled"), (account, 0, payment.Amount, "Cash paid")], payment.BranchId);
    }

    public async Task<(bool Success, string Error)> PostSalaryAccrualAsync(int companyId, EmployeeSalary salary, int? branchId = null)
    {
        return await PostAsync(companyId, salary.PaymentDate, "Salary Payment", salary.ReferenceNo ?? salary.ForMonth.ToString("yyyy-MM"),
            $"Salary accrual for {salary.ForMonth:yyyy-MM}",
            [(SalaryExpense, salary.Amount, 0, "Salary accrued"), (SalaryPayable, 0, salary.Amount, "Salary payable")], branchId);
    }

    public async Task<(bool Success, string Error)> PostSalaryDirectAsync(int companyId, EmployeeSalary salary, int? branchId = null)
    {
        var account = salary.PaymentMode == PaymentMode.Bank ? Bank : Cash;
        return await PostAsync(companyId, salary.PaymentDate, "Salary Payment", salary.ReferenceNo ?? salary.ForMonth.ToString("yyyy-MM"),
            $"Salary payment for {salary.ForMonth:yyyy-MM}",
            [(SalaryExpense, salary.Amount, 0, "Salary expense"), (account, 0, salary.Amount, "Salary paid")], branchId);
    }

    public Task<(bool Success, string Error)> PostAssetPaymentAsync(Payment payment)
    {
        return PostAsync(payment.CompanyId, payment.PaymentDate, "Asset Payment", payment.PaymentNo,
            $"Asset payment {payment.PaymentNo}",
            [(AssetPayable, payment.Amount, 0, "Asset payable settled"),
             (payment.AccountType == PaymentAccountType.Cash ? Cash : Bank, 0, payment.Amount, "Cash paid")]);
    }

    public Task<(bool Success, string Error)> PostSalaryPaymentAsync(Payment payment)
    {
        return PostAsync(payment.CompanyId, payment.PaymentDate, "Salary Payment", payment.PaymentNo,
            $"Salary payment {payment.PaymentNo}",
            [(SalaryPayable, payment.Amount, 0, "Salary payable settled"),
             (payment.AccountType == PaymentAccountType.Cash ? Cash : Bank, 0, payment.Amount, "Salary paid")], payment.BranchId);
    }

    public async Task<(bool Success, string Error)> PostAssetAcquisitionAsync(AssetRegister asset, AssetAcquisition acquisition)
    {
        // A company with no chart of accounts yet has to be seeded before any of the codes below can
        // be resolved, otherwise a first ever asset fails on a missing account instead of creating it.
        var ensure = await EnsureDefaultsAsync(asset.CompanyId);
        if (!ensure.Success)
        {
            return ensure;
        }

        if (_db.ChangeTracker.Entries<ChartOfAccount>().Any(e => e.State == EntityState.Added))
        {
            await _db.SaveChangesAsync();
        }

        var paid = Math.Round(Math.Min(acquisition.AmountPaid, asset.Cost), 2);
        var payable = Math.Round(asset.Cost - paid, 2);

        var lines = new List<(int AccountId, decimal Debit, decimal Credit, string? Note)>();

        if (asset.Cost > 0)
        {
            var fixedAssets = await ResolveAccountIdAsync(asset.CompanyId, FixedAssets);
            if (!fixedAssets.Success)
            {
                return (false, fixedAssets.Error);
            }

            lines.Add((fixedAssets.AccountId, asset.Cost, 0, asset.Name));
        }

        if (paid > 0)
        {
            // Credit the account the money actually leaves from rather than a generic cash or bank
            // control account, so the ledger agrees with the company's cash and bank balances.
            var fund = await ResolveFundAccountAsync(asset, acquisition);
            if (!fund.Success)
            {
                return (false, fund.Error);
            }

            lines.Add((fund.AccountId, 0, paid, "Asset payment"));
        }

        if (payable > 0)
        {
            var assetPayable = await ResolveAccountIdAsync(asset.CompanyId, AssetPayable);
            if (!assetPayable.Success)
            {
                return (false, assetPayable.Error);
            }

            lines.Add((assetPayable.AccountId, 0, payable, "Asset payable"));
        }

        return await StagePostAsync(asset.CompanyId, acquisition.AcquisitionDate, "Asset Acquisition", asset.AssetNo,
            $"Asset acquisition {asset.AssetNo}", lines, asset.BranchId);
    }

    /// <summary>
    /// Resolves the account the asset payment actually leaves from. A cash account carries its own
    /// chart code, so the ledger can follow that specific till. A bank account has no code of its own
    /// in this schema, so it posts to the bank control account after the bank account itself has been
    /// confirmed as real for this company and branch.
    /// </summary>
    private async Task<(bool Success, string Error, int AccountId)> ResolveFundAccountAsync(AssetRegister asset, AssetAcquisition acquisition)
    {
        if (acquisition.PaymentType == AcquisitionPaymentType.Cash)
        {
            if (!acquisition.CashAccountId.HasValue)
            {
                return (false, "Select the cash account the payment is made from.", 0);
            }

            var cashAccount = await _db.CashAccounts
                .FirstOrDefaultAsync(c => c.Id == acquisition.CashAccountId.Value && c.CompanyId == asset.CompanyId);
            if (cashAccount is null || cashAccount.BranchId != asset.BranchId)
            {
                return (false, "The selected cash account is not an account of this company and branch.", 0);
            }

            return await ResolveAccountIdAsync(asset.CompanyId, cashAccount.AccountCode);
        }

        if (!acquisition.BankAccountId.HasValue)
        {
            return (false, "Select the bank account the payment is made from.", 0);
        }

        var bankAccount = await _db.BankAccounts
            .FirstOrDefaultAsync(b => b.Id == acquisition.BankAccountId.Value && b.CompanyId == asset.CompanyId);
        if (bankAccount is null || bankAccount.BranchId != asset.BranchId)
        {
            return (false, "The selected bank account is not an account of this company and branch.", 0);
        }

        return await ResolveAccountIdAsync(asset.CompanyId, Bank);
    }

    private async Task<(bool Success, string Error, int AccountId)> ResolveAccountIdAsync(int companyId, string code)
    {
        var account = _db.ChartOfAccounts.Local
            .FirstOrDefault(a => a.CompanyId == companyId && a.AccountCode == code && a.IsActive)
            ?? await _db.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == code && a.IsActive);
        if (account is null)
        {
            return (false, $"Chart of Accounts entry '{code}' is missing for this company.", 0);
        }

        if (!account.IsPostable)
        {
            return (false, $"Account '{code}' is a parent/group account and cannot receive postings.", 0);
        }

        return (true, string.Empty, account.Id);
    }

    public async Task<(bool Success, string Error)> PostAssetDepreciationAsync(int companyId, string periodKey, decimal amount, string note)
    {
        if (amount <= 0)
        {
            return (true, string.Empty);
        }

        var periodDate = LastDayOfMonth(periodKey);
        return await PostAsync(companyId, periodDate, "Depreciation", periodKey,
            $"Depreciation for period {periodKey}",
            [(DepreciationExpense, amount, 0, string.IsNullOrWhiteSpace(note) ? null : note.Trim()),
             (AccumulatedDepreciation, 0, amount, "Accumulated depreciation")]);
    }

    public async Task<(bool Success, string Error)> PostAssetDisposalAsync(AssetRegister asset, AssetDisposal disposal)
    {
        decimal accumulated = Math.Round(asset.AccumulatedDepreciation, 2);
        decimal bookValue = Math.Round(asset.BookValue, 2);
        decimal saleValue = Math.Round(disposal.SaleValue, 2);
        decimal gainLoss = Math.Round(saleValue - bookValue, 2);

        var lines = new List<(string Code, decimal Debit, decimal Credit, string? Note)>();
        if (accumulated > 0)
        {
            lines.Add((AccumulatedDepreciation, accumulated, 0, "Accumulated depreciation written off"));
        }

        if (saleValue > 0)
        {
            lines.Add((Cash, saleValue, 0, "Proceeds from asset disposal"));
        }

        if (gainLoss > 0)
        {
            lines.Add((GainOnDisposal, 0, gainLoss, "Gain on disposal"));
        }
        else if (gainLoss < 0)
        {
            lines.Add((LossOnDisposal, Math.Abs(gainLoss), 0, "Loss on disposal"));
        }

        lines.Add((FixedAssets, 0, asset.Cost, "Fixed asset written off"));

        return await PostAsync(asset.CompanyId, disposal.DisposalDate, "Asset Disposal", asset.AssetNo,
            $"Disposal of {asset.Name}", lines, asset.BranchId);
    }

    // ---- Manual journal (UI vouchers) ----

    public async Task<(bool Success, string Error, JournalEntry? Entry)> PostManualAsync(
        int companyId, DateTime entryDate, string description, List<(int AccountId, decimal Debit, decimal Credit, string? Note)> lines)
    {
        var result = await StagePostAsync(companyId, entryDate, "Manual Journal", null, description, lines);
        if (!result.Success)
        {
            return (false, result.Error, null);
        }

        var entry = _db.JournalEntries.Local.LastOrDefault(e => e.SourceModule == "Manual Journal" && e.CompanyId == companyId && e.EntryDate.Date == entryDate.Date);
        return (true, string.Empty, entry);
    }

    private async Task<(bool Success, string Error)> PostAsync(int companyId, DateTime entryDate,
        string sourceModule, string? sourceReference, string description, List<(string Code, decimal Debit, decimal Credit, string? Note)> lines, int? branchId = null)
    {
        var ensure = await EnsureDefaultsAsync(companyId);
        if (!ensure.Success)
        {
            return ensure;
        }

        // Persist newly-seeded default accounts so their generated Ids are
        // available when the caller's SaveChangesAsync commits this journal.
        if (_db.ChangeTracker.Entries<ChartOfAccount>().Any(e => e.State == EntityState.Added))
        {
            await _db.SaveChangesAsync();
        }

        var accountIds = new List<(int AccountId, decimal Debit, decimal Credit, string? Note)>();
        foreach (var (code, debit, credit, note) in lines)
        {
            if (debit < 0 || credit < 0)
            {
                return (false, "Journal amounts must be valid numbers.");
            }

            var account = _db.ChartOfAccounts.Local
                .FirstOrDefault(a => a.CompanyId == companyId && a.AccountCode == code && a.IsActive)
                ?? await _db.ChartOfAccounts
                    .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == code && a.IsActive);
            if (account is null)
            {
                return (false, $"Chart of Accounts entry '{code}' is missing for this company.");
            }

            if (!account.IsPostable)
            {
                return (false, $"Account '{code}' is a parent/group account and cannot receive postings.");
            }

            accountIds.Add((account.Id, Math.Round(debit, 2), Math.Round(credit, 2), note));
        }

        return await StagePostAsync(companyId, entryDate, sourceModule, sourceReference, description, accountIds, branchId);
    }

    private async Task<(bool Success, string Error)> StagePostAsync(int companyId, DateTime entryDate,
        string sourceModule, string? sourceReference, string description, List<(int AccountId, decimal Debit, decimal Credit, string? Note)> raw, int? branchId = null)
    {
        entryDate = entryDate == default ? DateTime.Today : entryDate;

        var lines = raw
            .Where(l => l.Debit > 0 || l.Credit > 0)
            .Select(l => (
                AccountId: l.AccountId,
                Debit: Math.Round(decimal.Max(l.Debit, 0), 2),
                Credit: Math.Round(decimal.Max(l.Credit, 0), 2),
                Note: l.Note))
            .ToList();

        if (lines.Count == 0)
        {
            return (false, "Journal entry must have at least one line with a debit or credit amount.");
        }

        foreach (var line in lines)
        {
            if (line.Debit > 0 && line.Credit > 0)
            {
                return (false, "A journal line cannot contain both a debit and a credit.");
            }

            if (!await _db.ChartOfAccounts.AnyAsync(a => a.Id == line.AccountId && a.CompanyId == companyId))
            {
                return (false, "One or more selected accounts do not belong to the company.");
            }

            if (!await _db.ChartOfAccounts.AnyAsync(a => a.Id == line.AccountId && a.IsPostable))
            {
                return (false, "Parent/group accounts cannot receive postings. Select a postable (leaf) account.");
            }
        }

        var totalDebit = Math.Round(lines.Sum(l => l.Debit), 2);
        var totalCredit = Math.Round(lines.Sum(l => l.Credit), 2);
        if (totalDebit != totalCredit)
        {
            return (false, "Debit and credit totals must balance.");
        }

        var period = await ResolvePeriodAsync(companyId, entryDate);
        if (period.Period is null)
        {
            return (false, period.PeriodError ?? string.Empty);
        }

        var count = await _db.JournalEntries
            .CountAsync(e => e.CompanyId == companyId && e.EntryDate.Date == entryDate.Date);
        var entryNo = $"JR-{entryDate:yyyyMMdd}-{(count + 1):D3}";

        var entry = new JournalEntry
        {
            CompanyId = companyId,
            BranchId = branchId,
            EntryNo = entryNo,
            EntryDate = entryDate,
            AccountingPeriodId = period.Period.Id > 0 ? period.Period.Id : null,
            AccountingPeriod = period.Period,
            PeriodKey = period.Period.PeriodCode,
            SourceModule = sourceModule,
            SourceReference = sourceReference,
            Description = description,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit
        };

        _db.JournalEntries.Add(entry);

        foreach (var line in lines)
        {
            _db.JournalEntryDetails.Add(new JournalEntryDetail
            {
                JournalEntry = entry,
                AccountId = line.AccountId,
                Debit = line.Debit,
                Credit = line.Credit,
                Note = line.Note
            });
        }

        return (true, string.Empty);
    }

    private async Task<(AccountingPeriod? Period, string? PeriodError)> ResolvePeriodAsync(int companyId, DateTime date)
    {
        var years = await _db.FinancialYears
            .Include(f => f.AccountingPeriods)
            .OrderBy(f => f.StartDate)
            .ToListAsync();

        var financialYear = years.FirstOrDefault(f => f.StartDate.Date <= date.Date && date.Date <= f.EndDate.Date);

        if (financialYear is null)
        {
            var latest = years.LastOrDefault();
            DateTime start;
            if (latest is not null && date.Date > latest.EndDate.Date)
            {
                start = latest.EndDate.AddDays(1);
            }
            else
            {
                start = new DateTime(date.Year, 1, 1);
            }

            financialYear = new FinancialYear
            {
                YearCode = $"FY{start.Year}-{(start.Year + 1) % 100:00}",
                Name = $"{start.Year}-{start.Year + 1}",
                StartDate = start.Date,
                EndDate = start.AddYears(1).AddDays(-1).Date,
                IsClosed = false
            };
            _db.FinancialYears.Add(financialYear);
        }

        if (financialYear.IsClosed)
        {
            return (null, "Cannot post into a closed financial year.");
        }

        var period = financialYear.AccountingPeriods
            .FirstOrDefault(p => p.StartDate.Date <= date.Date && date.Date <= p.EndDate.Date);

        if (period is null)
        {
            var start = new DateTime(date.Year, date.Month, 1);
            var end = start.AddMonths(1).AddDays(-1);

            period = new AccountingPeriod
            {
                FinancialYearId = financialYear.Id,
                PeriodCode = date.ToString("yyyyMM"),
                Name = date.ToString("MMMM yyyy"),
                StartDate = start.Date,
                EndDate = end.Date,
                Status = AccountingPeriodStatus.Open
            };
            financialYear.AccountingPeriods.Add(period);
        }

        if (period.Status == AccountingPeriodStatus.Closed)
        {
            return (null, $"Cannot post into a closed accounting period {period.PeriodCode}.");
        }

        return (period, null);
    }

    private static DateTime LastDayOfMonth(string periodKey)
    {
        if (DateTime.TryParseExact(periodKey, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsed))
        {
            return new DateTime(parsed.Year, parsed.Month, DateTime.DaysInMonth(parsed.Year, parsed.Month));
        }

        return DateTime.Today;
    }
}