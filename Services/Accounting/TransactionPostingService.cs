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
    private const string SalaryExpense = "5100";
    private const string DepreciationExpense = "5200";
    private const string OperatingExpense = "5300";
    private const string GainOnDisposal = "5400";
    private const string LossOnDisposal = "5410";

    // Default chart of accounts seeded per company so every posting always resolves.
    private static readonly List<(string Code, string Name, AccountType Type, AccountNormalBalance Balance)> DefaultAccounts =
    [
        (Cash, "Cash", AccountType.Asset, AccountNormalBalance.Debit),
        (Bank, "Bank", AccountType.Asset, AccountNormalBalance.Debit),
        (AccountsReceivable, "Accounts Receivable", AccountType.Asset, AccountNormalBalance.Debit),
        (Inventory, "Inventory", AccountType.Asset, AccountNormalBalance.Debit),
        (FixedAssets, "Fixed Assets", AccountType.Asset, AccountNormalBalance.Debit),
        (AccumulatedDepreciation, "Accumulated Depreciation", AccountType.Asset, AccountNormalBalance.Credit),
        (AccountsPayable, "Accounts Payable", AccountType.Liability, AccountNormalBalance.Credit),
        (ExpensePayable, "Expense Payable", AccountType.Liability, AccountNormalBalance.Credit),
        (SalaryPayable, "Salary Payable", AccountType.Liability, AccountNormalBalance.Credit),
        (AssetPayable, "Asset Payable", AccountType.Liability, AccountNormalBalance.Credit),
        (Equity, "Opening Balance Equity", AccountType.Equity, AccountNormalBalance.Credit),
        (ProductRevenue, "Product Sales Revenue", AccountType.Revenue, AccountNormalBalance.Credit),
        (SoftwareRevenue, "Software Revenue", AccountType.Revenue, AccountNormalBalance.Credit),
        (ServiceRevenue, "Service Revenue", AccountType.Revenue, AccountNormalBalance.Credit),
        (GainOnDisposal, "Gain on Asset Disposal", AccountType.Revenue, AccountNormalBalance.Credit),
        (CostOfGoodsSold, "Cost of Goods Sold", AccountType.Expense, AccountNormalBalance.Debit),
        (SalaryExpense, "Salary Expense", AccountType.Expense, AccountNormalBalance.Debit),
        (DepreciationExpense, "Depreciation Expense", AccountType.Expense, AccountNormalBalance.Debit),
        (OperatingExpense, "Operating Expenses", AccountType.Expense, AccountNormalBalance.Debit),
        (LossOnDisposal, "Loss on Asset Disposal", AccountType.Expense, AccountNormalBalance.Debit)
    ];

    public async Task<(bool Success, string Error)> EnsureDefaultsAsync(int companyId)
    {
        if (!await _db.Companies.AnyAsync(c => c.Id == companyId))
        {
            return (false, "Company does not exist.");
        }

        foreach (var (code, name, type, normal) in DefaultAccounts)
        {
            var exists = await _db.ChartOfAccounts.AnyAsync(a => a.CompanyId == companyId && a.AccountCode == code);
            if (!exists)
            {
                _db.ChartOfAccounts.Add(new ChartOfAccount
                {
                    CompanyId = companyId,
                    AccountCode = code,
                    AccountName = name,
                    AccountType = type,
                    NormalBalance = normal,
                    IsActive = true
                });
            }
        }

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

    public async Task<(bool Success, string Error)> PostSalesInvoiceAsync(SalesInvoice invoice, IReadOnlyDictionary<int, Product> products)
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

        var cogs = 0m;
        foreach (var line in invoice.Lines.Where(l => l.ItemType == SalesItemType.Product && l.ProductId.HasValue))
        {
            if (products.TryGetValue(line.ProductId!.Value, out var product))
            {
                cogs += line.Quantity * product.CostPrice;
            }
        }

        cogs = Math.Round(cogs, 2);
        if (cogs > 0)
        {
            lines.Add((CostOfGoodsSold, cogs, 0, "Cost of goods sold"));
            lines.Add((Inventory, 0, cogs, "Inventory reduction"));
        }

        return await PostAsync(companyId, invoice.InvoiceDate, "Sales Invoice", invoice.InvoiceNo,
            $"Sales invoice {invoice.InvoiceNo}", lines, invoice.BranchId);
    }

    public async Task<(bool Success, string Error)> PostSalesReturnAsync(SalesReturn salesReturn, SalesInvoice invoice, IReadOnlyDictionary<int, Product> products)
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

        var cogs = 0m;
        foreach (var line in salesReturn.Lines.Where(l => l.ItemType == SalesItemType.Product && l.ProductId.HasValue))
        {
            if (products.TryGetValue(line.ProductId!.Value, out var product))
            {
                cogs += line.Quantity * product.CostPrice;
            }
            else if (invoice.Lines.FirstOrDefault(l => l.ItemType == SalesItemType.Product && l.ProductId == line.ProductId) is { } invLine)
            {
                cogs += line.Quantity * invLine.UnitPrice;
            }
        }

        cogs = Math.Round(cogs, 2);
        if (cogs > 0)
        {
            lines.Add((Inventory, cogs, 0, "Inventory restored"));
            lines.Add((CostOfGoodsSold, 0, cogs, "COGS reversal"));
        }

        return await PostAsync(salesReturn.CompanyId, salesReturn.ReturnDate, "Sales Return", salesReturn.ReturnNo,
            $"Sales return {salesReturn.ReturnNo}", lines, salesReturn.BranchId);
    }

    public async Task<(bool Success, string Error)> PostPurchaseInvoiceAsync(PurchaseInvoice invoice)
    {
        var total = Math.Round(invoice.Lines.Sum(l => l.Quantity * l.UnitPrice), 2);
        return await PostAsync(invoice.CompanyId, invoice.InvoiceDate, "Purchase Invoice", invoice.InvoiceNo,
            $"Purchase invoice {invoice.InvoiceNo}",
            [(Inventory, total, 0, "Purchased goods received"), (AccountsPayable, 0, total, "Supplier payable")], invoice.BranchId);
    }

    public async Task<(bool Success, string Error)> PostPurchaseReturnAsync(PurchaseReturn purchaseReturn)
    {
        var total = Math.Round(purchaseReturn.Lines.Sum(l => l.Quantity * l.UnitCost), 2);
        return await PostAsync(purchaseReturn.CompanyId, purchaseReturn.ReturnDate, "Purchase Return", purchaseReturn.ReturnNo,
            $"Purchase return {purchaseReturn.ReturnNo}",
            [(AccountsPayable, total, 0, "Supplier credit for returned goods"), (Inventory, 0, total, "Returned goods out")], purchaseReturn.BranchId);
    }

    public async Task<(bool Success, string Error)> PostCustomerPaymentAsync(Payment payment)
    {
        var account = payment.AccountType == PaymentAccountType.Cash ? Cash : Bank;
        return await PostAsync(payment.CompanyId, payment.PaymentDate, "Customer Payment", payment.PaymentNo,
            $"Customer payment {payment.PaymentNo}",
            [(account, payment.Amount, 0, "Cash received"), (AccountsReceivable, 0, payment.Amount, "Receivable settled")], payment.BranchId);
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
        var lines = new List<(string Code, decimal Debit, decimal Credit, string? Note)>
        {
            (FixedAssets, asset.Cost, 0, asset.Name)
        };

        var paid = Math.Round(Math.Min(acquisition.AmountPaid, asset.Cost), 2);
        if (paid > 0)
        {
            var account = acquisition.PaymentType switch
            {
                AcquisitionPaymentType.Bank => Bank,
                AcquisitionPaymentType.Cash => Cash,
                _ => Cash
            };
            lines.Add((account, 0, paid, "Asset payment"));
        }

        var payable = Math.Round(asset.Cost - paid, 2);
        if (payable > 0)
        {
            lines.Add((AssetPayable, 0, payable, "Asset payable"));
        }

        return await PostAsync(asset.CompanyId, acquisition.AcquisitionDate, "Asset Acquisition", asset.AssetNo,
            $"Asset acquisition {asset.AssetNo}", lines, asset.BranchId);
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