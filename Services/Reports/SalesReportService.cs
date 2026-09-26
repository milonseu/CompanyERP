using CompanyERP.Data;
using CompanyERP.Entities.Payment;
using CompanyERP.Entities.Sales;
using CompanyERP.Interfaces.Services;
using CompanyERP.ViewModels.Reports;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Reports;

public class SalesReportService : ISalesReportService
{
    private readonly ApplicationDbContext _db;

    public SalesReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<SalesSummaryViewModel> GetSummaryAsync(int companyId, DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate?.Date ?? DateTime.MinValue.Date;
        var to = toDate?.Date ?? DateTime.MaxValue.Date;

        var invoices = await _db.SalesInvoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Customer)
            .Where(i => i.CompanyId == companyId && i.InvoiceDate >= from && i.InvoiceDate <= to)
            .ToListAsync();

        var rows = invoices
            .GroupBy(i => i.CustomerId)
            .Select(g =>
            {
                var customer = g.First().Customer;
                var total = g.Sum(i => i.Lines.Sum(l => l.Quantity * l.UnitPrice));
                var paid = g.Sum(i => Math.Min(i.AmountPaid, i.Lines.Sum(l => l.Quantity * l.UnitPrice)));
                return new SalesSummaryRow
                {
                    CustomerId = g.Key,
                    Customer = customer?.Name ?? "Unknown",
                    InvoiceCount = g.Count(),
                    Quantity = Math.Round(g.Sum(i => i.Lines.Sum(l => l.Quantity)), 2),
                    Sales = Math.Round(total, 2),
                    Paid = Math.Round(paid, 2)
                };
            })
            .OrderByDescending(r => r.Sales)
            .ToList();

        return new SalesSummaryViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            Rows = rows,
            TotalInvoices = rows.Sum(r => r.InvoiceCount),
            TotalQuantity = Math.Round(rows.Sum(r => r.Quantity), 2),
            TotalSales = Math.Round(rows.Sum(r => r.Sales), 2),
            TotalPaid = Math.Round(rows.Sum(r => r.Paid), 2)
        };
    }

    public async Task<SalesByProductViewModel> GetByProductAsync(int companyId, DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate?.Date ?? DateTime.MinValue.Date;
        var to = toDate?.Date ?? DateTime.MaxValue.Date;

        var rows = await _db.SalesInvoiceLines
            .AsNoTracking()
            .Include(l => l.SalesInvoice)
            .Include(l => l.Product)
                .ThenInclude(p => p!.Category)
            .Where(l => l.SalesInvoice!.CompanyId == companyId &&
                        l.SalesInvoice.InvoiceDate >= from && l.SalesInvoice.InvoiceDate <= to)
            .ToListAsync();

        var products = rows
            .Where(l => l.ItemType == SalesItemType.Product && l.ProductId.HasValue)
            .GroupBy(l => l.ProductId!.Value)
            .Select(g => new SalesByProductRow
            {
                Code = g.First().Product?.Code ?? "",
                Product = g.First().Product?.Name ?? "",
                Category = g.First().Product?.Category?.Name ?? "",
                Quantity = Math.Round(g.Sum(l => l.Quantity), 2),
                Amount = Math.Round(g.Sum(l => l.Quantity * l.UnitPrice), 2)
            })
            .OrderByDescending(r => r.Amount)
            .ToList();

        var categories = rows
            .Where(l => l.ItemType == SalesItemType.Product && l.ProductId.HasValue)
            .GroupBy(l => l.Product?.Category?.Name ?? "Unknown")
            .Select(g => new SalesByCategoryRow
            {
                Category = g.Key,
                Quantity = Math.Round(g.Sum(l => l.Quantity), 2),
                Amount = Math.Round(g.Sum(l => l.Quantity * l.UnitPrice), 2)
            })
            .OrderByDescending(r => r.Amount)
            .ToList();

        return new SalesByProductViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            Products = products,
            Categories = categories,
            TotalSales = Math.Round(products.Sum(r => r.Amount), 2)
        };
    }

    public async Task<OutstandingReceivableViewModel> GetOutstandingAsync(int companyId, DateTime? asOfDate)
    {
        var customers = await _db.Customers
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .ToListAsync();

        var invoices = await _db.SalesInvoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Where(i => i.CompanyId == companyId)
            .ToListAsync();

        var payments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId && p.Category == PaymentCategory.Customer)
            .ToListAsync();

        var rows = customers
            .Select(c =>
            {
                var invs = invoices.Where(i => i.CustomerId == c.Id).ToList();
                var sales = invs.Sum(i => i.Lines.Sum(l => l.Quantity * l.UnitPrice));
                var paid = invs.Sum(i => Math.Min(i.AmountPaid, i.Lines.Sum(l => l.Quantity * l.UnitPrice))) +
                           payments.Where(p => p.CustomerId == c.Id).Sum(p => p.Amount);
                return new OutstandingReceivableRow
                {
                    CustomerId = c.Id,
                    Customer = c.Name,
                    Invoices = invs.Count,
                    Sales = Math.Round(sales, 2),
                    Paid = Math.Round(paid, 2),
                    Outstanding = Math.Round(Math.Max(0, c.OpeningReceivable + sales - paid), 2)
                };
            })
            .Where(r => r.Outstanding > 0)
            .OrderByDescending(r => r.Outstanding)
            .ToList();

        return new OutstandingReceivableViewModel
        {
            AsOfDate = asOfDate,
            Rows = rows,
            TotalOutstanding = Math.Round(rows.Sum(r => r.Outstanding), 2)
        };
    }

    public async Task<TopSalesViewModel> GetTopAsync(int companyId, DateTime? fromDate, DateTime? toDate, int top = 10)
    {
        var from = fromDate?.Date ?? DateTime.MinValue.Date;
        var to = toDate?.Date ?? DateTime.MaxValue.Date;

        var rows = await _db.SalesInvoiceLines
            .AsNoTracking()
            .Include(l => l.SalesInvoice)
                .ThenInclude(i => i!.Customer)
            .Include(l => l.Product)
            .Where(l => l.SalesInvoice!.CompanyId == companyId &&
                        l.SalesInvoice.InvoiceDate >= from && l.SalesInvoice.InvoiceDate <= to)
            .ToListAsync();

        var customers = rows
            .GroupBy(l => l.SalesInvoice!.CustomerId)
            .Select(g => new
            {
                Name = g.First().SalesInvoice!.Customer?.Name ?? "Unknown",
                Amount = Math.Round(g.Sum(l => l.Quantity * l.UnitPrice), 2)
            })
            .OrderByDescending(x => x.Amount)
            .Take(top)
            .ToList();

        var products = rows
            .Where(l => l.ItemType == SalesItemType.Product && l.ProductId.HasValue)
            .GroupBy(l => l.ProductId!.Value)
            .Select(g => new
            {
                Name = g.First().Product?.Name ?? "Unknown",
                Quantity = Math.Round(g.Sum(l => l.Quantity), 2),
                Amount = Math.Round(g.Sum(l => l.Quantity * l.UnitPrice), 2)
            })
            .OrderByDescending(x => x.Amount)
            .Take(top)
            .ToList();

        var totalCustomer = customers.Sum(c => c.Amount);
        var totalProduct = products.Sum(p => p.Amount);

        return new TopSalesViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            Top = top,
            Customers = customers
                .Select((x, idx) => new TopCustomerRow
                {
                    Rank = idx + 1,
                    Customer = x.Name,
                    Amount = x.Amount,
                    Percent = totalCustomer == 0 ? 0 : Math.Round(x.Amount / totalCustomer * 100m, 2)
                })
                .ToList(),
            Products = products
                .Select((x, idx) => new TopProductRow
                {
                    Rank = idx + 1,
                    Product = x.Name,
                    Quantity = x.Quantity,
                    Amount = x.Amount,
                    Percent = totalProduct == 0 ? 0 : Math.Round(x.Amount / totalProduct * 100m, 2)
                })
                .ToList()
        };
    }

    public async Task<ServiceCompletionViewModel> GetServiceCompletionAsync(int companyId, DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate?.Date ?? DateTime.MinValue.Date;
        var to = toDate?.Date ?? DateTime.MaxValue.Date;

        var orders = await _db.ServiceOrders
            .AsNoTracking()
            .Include(o => o.Service)
            .Include(o => o.Deliveries)
            .Where(o => o.CompanyId == companyId && o.OrderDate >= from && o.OrderDate <= to)
            .ToListAsync();

        var rows = orders
            .GroupBy(o => o.ServiceId)
            .Select(g => new ServiceCompletionRow
            {
                Service = g.First().Service?.Name ?? "Unknown",
                Orders = g.Count(),
                OrderedQty = Math.Round(g.Sum(o => o.Quantity), 2),
                DeliveredQty = Math.Round(g.Sum(o => o.Deliveries.Sum(d => d.Quantity)), 2),
                Amount = Math.Round(g.Sum(o => o.Quantity * o.UnitPrice), 2),
                DeliveredPercent = 0
            })
            .OrderByDescending(r => r.Amount)
            .ToList();

        foreach (var r in rows)
        {
            r.DeliveredPercent = r.OrderedQty == 0 ? 0 : Math.Round(r.DeliveredQty / r.OrderedQty * 100m, 1);
        }

        return new ServiceCompletionViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            Pending = orders.Count(o => o.Status == ServiceOrderStatus.Pending),
            InProgress = orders.Count(o => o.Status == ServiceOrderStatus.InProgress),
            Delivered = orders.Count(o => o.Status == ServiceOrderStatus.Delivered),
            Cancelled = orders.Count(o => o.Status == ServiceOrderStatus.Cancelled),
            Rows = rows
        };
    }
}