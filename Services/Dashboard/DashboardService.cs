using CompanyERP.Data;
using CompanyERP.Entities.Payment;
using CompanyERP.Entities.Purchase;
using CompanyERP.Entities.Sales;
using CompanyERP.Interfaces.Services;
using CompanyERP.ViewModels.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;

    public DashboardService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<int> GetFirstCompanyIdAsync()
    {
        var companyId = await _db.Companies.AsNoTracking().OrderBy(c => c.Name).Select(c => (int?)c.Id).FirstOrDefaultAsync();
        return companyId ?? 1;
    }

    public async Task<DashboardViewModel> GetDashboardAsync(int companyId)
    {
        var vm = new DashboardViewModel { CompanyId = companyId };

        var company = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId);
        vm.CompanyName = company?.Name ?? "CompanyERP";

        var sales = await _db.SalesInvoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Customer)
            .Where(i => i.CompanyId == companyId)
            .ToListAsync();

        var purchases = await _db.PurchaseInvoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Supplier)
            .Where(i => i.CompanyId == companyId)
            .ToListAsync();

        var payments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId)
            .ToListAsync();

        var customers = await _db.Customers.AsNoTracking().Where(c => c.CompanyId == companyId).ToListAsync();
        var suppliers = await _db.Suppliers.AsNoTracking().Where(s => s.CompanyId == companyId).ToListAsync();
        var products = await _db.Products.AsNoTracking().Where(p => p.CompanyId == companyId).ToListAsync();
        var employees = await _db.Employees.AsNoTracking().Where(e => e.CompanyId == companyId).ToListAsync();
        var categories = await _db.ProductCategories.AsNoTracking().Where(c => c.CompanyId == companyId).ToListAsync();
        var expenses = await _db.ExpenseEntries.AsNoTracking().Where(e => e.CompanyId == companyId).ToListAsync();
        var assets = await _db.AssetRegisters.AsNoTracking().Where(a => a.CompanyId == companyId).ToListAsync();
        var serviceOrders = await _db.ServiceOrders.AsNoTracking().Where(o => o.CompanyId == companyId).ToListAsync();
        var salaryPayments = await _db.SalaryPayments
            .AsNoTracking()
            .Include(s => s.Employee)
            .Where(s => s.Employee != null && s.Employee.CompanyId == companyId)
            .ToListAsync();

        var productIds = products.Select(p => p.Id).ToHashSet();
        var balances = await _db.StockBalances.AsNoTracking().Where(b => productIds.Contains(b.ProductId)).ToListAsync();

        var cashOpening = await _db.CashAccounts.AsNoTracking().Where(c => c.CompanyId == companyId).SumAsync(c => (decimal?)c.OpeningBalance) ?? 0;
        var bankOpening = await _db.BankAccounts.AsNoTracking().Where(b => b.CompanyId == companyId).SumAsync(b => (decimal?)b.OpeningBalance) ?? 0;

        // Totals
        var totalSales = sales.Sum(i => i.Lines.Sum(l => l.Quantity * l.UnitPrice));
        var totalPurchases = purchases.Sum(i => i.Lines.Sum(l => l.Quantity * l.UnitPrice));
        vm.TotalSales = Math.Round(totalSales, 2);
        vm.TotalPurchases = Math.Round(totalPurchases, 2);
        vm.TotalExpenses = Math.Round(expenses.Sum(e => e.Amount), 2);
        vm.StockValue = Math.Round(balances.Sum(b => b.Quantity * b.AverageCost), 2);
        vm.AssetBookValue = Math.Round(assets.Sum(a => Math.Max(0, a.Cost - a.AccumulatedDepreciation)), 2);
        vm.PendingServiceCount = serviceOrders.Count(o => o.Status == ServiceOrderStatus.Pending);

        // Counts
        vm.CustomerCount = customers.Count;
        vm.SupplierCount = suppliers.Count;
        vm.EmployeeCount = employees.Count;
        vm.ProductCount = products.Count;
        vm.AssetCount = assets.Count;

        // Receivables / payables
        var invoiceTotal = (SalesInvoice i) => i.Lines.Sum(l => l.Quantity * l.UnitPrice);
        var purchaseTotal = (PurchaseInvoice i) => i.Lines.Sum(l => l.Quantity * l.UnitPrice);

        var receivableByCustomer = customers.ToDictionary(c => c.Id, c =>
        {
            var invoices = sales.Where(i => i.CustomerId == c.Id).ToList();
            var sold = invoices.Sum(invoiceTotal);
            var paid = invoices.Sum(i => Math.Min(i.AmountPaid, invoiceTotal(i))) +
                       payments.Where(p => p.CustomerId == c.Id).Sum(p => p.Amount);
            return Math.Max(0, c.OpeningReceivable + sold - paid);
        });

        var payableBySupplier = suppliers.ToDictionary(s => s.Id, s =>
        {
            var invoices = purchases.Where(i => i.SupplierId == s.Id).ToList();
            var bought = invoices.Sum(purchaseTotal);
            var paid = payments.Where(p => p.SupplierId == s.Id).Sum(p => p.Amount);
            return Math.Max(0, s.OpeningPayable + bought - paid);
        });

        vm.ReceivablesOutstanding = Math.Round(receivableByCustomer.Values.Sum(), 2);
        vm.PayablesOutstanding = Math.Round(payableBySupplier.Values.Sum(), 2);

        // Cash & bank balance
        var receipts = payments.Where(p => p.Category == PaymentCategory.Customer).Sum(p => p.Amount);
        var outflows = payments.Where(p => p.Category != PaymentCategory.Customer).Sum(p => p.Amount);
        vm.CashBankBalance = Math.Round(Math.Max(0, cashOpening + bankOpening + receipts - outflows), 2);

        vm.Monthly = BuildMonthly(sales, purchases, invoiceTotal, purchaseTotal);

        // Sales by category (product lines only)
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c.Name);
        var productLookup = products.ToDictionary(p => p.Id, p => (p.CategoryId, p.Name));
        vm.CategorySales = sales
            .SelectMany(i => i.Lines)
            .Where(l => l.ProductId.HasValue && productLookup.ContainsKey(l.ProductId.Value))
            .GroupBy(l => categoryLookup.GetValueOrDefault(productLookup[l.ProductId!.Value].CategoryId, "Other"))
            .Select(g => new CategorySlice
            {
                Label = g.Key,
                Amount = Math.Round(g.Sum(l => l.Quantity * l.UnitPrice), 2)
            })
            .OrderByDescending(s => s.Amount)
            .Take(6)
            .ToList();

        // Recent sales
        vm.RecentSales = sales
            .OrderByDescending(i => i.InvoiceDate)
            .Take(6)
            .Select(i =>
            {
                var total = invoiceTotal(i);
                var status = total <= 0 ? "Cancelled" : i.AmountPaid >= total ? "Paid" : i.AmountPaid > 0 ? "Partial" : "Due";
                return new RecentInvoiceRow
                {
                    InvoiceNo = i.InvoiceNo,
                    Date = i.InvoiceDate,
                    Party = i.Customer?.Name ?? "Unknown",
                    Amount = Math.Round(total, 2),
                    Status = status
                };
            })
            .ToList();

        // Recent payments
        var expenseLookup = expenses.ToDictionary(e => e.Id, e => e.Description);
        var salaryLookup = salaryPayments.ToDictionary(s => s.Id, s => s.Employee?.Name ?? "");
        var assetLookup = assets.ToDictionary(a => a.Id, a => a.Name);
        var customerLookup = customers.ToDictionary(c => c.Id, c => c.Name);
        var supplierLookup = suppliers.ToDictionary(s => s.Id, s => s.Name);

        vm.RecentPayments = payments
            .OrderByDescending(p => p.PaymentDate)
            .Take(6)
            .Select(p => new RecentPaymentRow
            {
                PaymentNo = p.PaymentNo,
                Date = p.PaymentDate,
                Category = p.Category.ToString(),
                Party = ResolveParty(p, customerLookup, supplierLookup, expenseLookup, salaryLookup, assetLookup),
                Amount = Math.Round(p.Amount, 2)
            })
            .ToList();

        // Low stock alerts (per product across warehouses)
        var qtyByProduct = balances
            .GroupBy(b => b.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(b => b.Quantity));

        vm.LowStock = products
            .Where(p => qtyByProduct.ContainsKey(p.Id) && p.MinimumStockLevel > 0)
            .Select(p => new LowStockDashboardRow
            {
                Code = p.Code,
                Product = p.Name,
                OnHand = Math.Round(qtyByProduct[p.Id], 2),
                Minimum = p.MinimumStockLevel
            })
            .Where(r => r.OnHand <= r.Minimum)
            .OrderByDescending(r => r.Shortfall)
            .Take(6)
            .ToList();

        // Top receivables / payables
        vm.TopReceivables = receivableByCustomer
            .Select(kv => new ReceivableDashboardRow
            {
                Customer = customerLookup.GetValueOrDefault(kv.Key, "Unknown"),
                Outstanding = Math.Round(kv.Value, 2)
            })
            .Where(r => r.Outstanding > 0)
            .OrderByDescending(r => r.Outstanding)
            .Take(6)
            .ToList();

        vm.TopPayables = payableBySupplier
            .Select(kv => new PayableDashboardRow
            {
                Supplier = supplierLookup.GetValueOrDefault(kv.Key, "Unknown"),
                Outstanding = Math.Round(kv.Value, 2)
            })
            .Where(r => r.Outstanding > 0)
            .OrderByDescending(r => r.Outstanding)
            .Take(6)
            .ToList();

        return vm;
    }

    private static List<MonthlyPoint> BuildMonthly(
        List<SalesInvoice> sales,
        List<PurchaseInvoice> purchases,
        Func<SalesInvoice, decimal> invoiceTotal,
        Func<PurchaseInvoice, decimal> purchaseTotal)
    {
        var months = new List<DateTime>();
        var today = DateTime.Today;
        for (int i = 7; i >= 0; i--)
        {
            months.Add(new DateTime(today.Year, today.Month, 1).AddMonths(-i));
        }

        var salesByMonth = GroupByMonth(sales, i => i.InvoiceDate, invoiceTotal);
        var purchaseByMonth = GroupByMonth(purchases, i => i.InvoiceDate, purchaseTotal);

        return months.Select(m => new MonthlyPoint
        {
            Label = m.ToString("MMM"),
            Sales = Math.Round(salesByMonth.GetValueOrDefault(m), 2),
            Purchases = Math.Round(purchaseByMonth.GetValueOrDefault(m), 2)
        }).ToList();
    }

    private static Dictionary<DateTime, decimal> GroupByMonth<T>(
        List<T> items,
        Func<T, DateTime> dateSelector,
        Func<T, decimal> amountSelector)
    {
        return items
            .GroupBy(i => new DateTime(dateSelector(i).Year, dateSelector(i).Month, 1))
            .ToDictionary(g => g.Key, g => g.Sum(amountSelector));
    }

    private static string ResolveParty(
        Payment p,
        Dictionary<int, string> customerLookup,
        Dictionary<int, string> supplierLookup,
        Dictionary<int, string> expenseLookup,
        Dictionary<int, string> salaryLookup,
        Dictionary<int, string> assetLookup)
    {
        return p.Category switch
        {
            PaymentCategory.Customer => customerLookup.GetValueOrDefault(p.CustomerId ?? 0, "Unknown"),
            PaymentCategory.Supplier => supplierLookup.GetValueOrDefault(p.SupplierId ?? 0, "Unknown"),
            PaymentCategory.Expense => expenseLookup.GetValueOrDefault(p.ExpenseEntryId ?? 0, "Expense"),
            PaymentCategory.Salary => salaryLookup.GetValueOrDefault(p.SalaryPaymentId ?? 0, "Salary"),
            PaymentCategory.Asset => assetLookup.GetValueOrDefault(p.AssetRegisterId ?? 0, "Asset"),
            _ => "Unknown"
        };
    }
}