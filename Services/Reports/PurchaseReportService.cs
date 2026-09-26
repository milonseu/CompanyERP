using CompanyERP.Data;
using CompanyERP.Entities.Purchase;
using CompanyERP.Interfaces.Services;
using CompanyERP.ViewModels.Reports;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Reports;

public class PurchaseReportService : IPurchaseReportService
{
    private readonly ApplicationDbContext _db;

    public PurchaseReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PurchaseSummaryViewModel> GetSummaryAsync(int companyId, DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate?.Date ?? DateTime.MinValue.Date;
        var to = toDate?.Date ?? DateTime.MaxValue.Date;

        var invoices = await _db.PurchaseInvoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Supplier)
            .Where(i => i.CompanyId == companyId && i.InvoiceDate >= from && i.InvoiceDate <= to)
            .ToListAsync();

        var rows = invoices
            .GroupBy(i => i.SupplierId)
            .Select(g => new PurchaseSummaryRow
            {
                SupplierId = g.Key,
                Supplier = g.First().Supplier?.Name ?? "Unknown",
                InvoiceCount = g.Count(),
                Quantity = Math.Round(g.Sum(i => i.Lines.Sum(l => l.Quantity)), 2),
                Purchases = Math.Round(g.Sum(i => i.Lines.Sum(l => l.Quantity * l.UnitPrice)), 2)
            })
            .OrderByDescending(r => r.Purchases)
            .ToList();

        return new PurchaseSummaryViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            Rows = rows,
            TotalInvoices = rows.Sum(r => r.InvoiceCount),
            TotalQuantity = Math.Round(rows.Sum(r => r.Quantity), 2),
            TotalPurchases = Math.Round(rows.Sum(r => r.Purchases), 2)
        };
    }

    public async Task<ProcurementAgingViewModel> GetProcurementAgingAsync(int companyId)
    {
        var requests = await _db.PurchaseRequests
            .AsNoTracking()
            .Include(r => r.Lines)
            .Where(r => r.CompanyId == companyId)
            .ToListAsync();

        var orders = await _db.PurchaseOrders
            .AsNoTracking()
            .Include(o => o.Lines)
            .Include(o => o.Supplier)
            .Where(o => o.CompanyId == companyId)
            .ToListAsync();

        var today = DateTime.Today;

        var requestRows = requests
            .Where(r => r.Status != PurchaseRequestStatus.Cancelled &&
                        r.Status != PurchaseRequestStatus.Converted)
            .Select(r => new ProcurementAgingRow
            {
                DocNo = r.RequestNo,
                Date = r.RequestDate,
                DaysOld = (int)(today - r.RequestDate.Date).TotalDays,
                Status = r.Status.ToString(),
                Supplier = "",
                LineCount = r.Lines.Count,
                Quantity = Math.Round(r.Lines.Sum(l => l.Quantity), 2)
            })
            .OrderByDescending(r => r.DaysOld)
            .ToList();

        var orderRows = orders
            .Where(o => o.Status != PurchaseOrderStatus.Cancelled &&
                        o.Status != PurchaseOrderStatus.Received)
            .Select(o => new ProcurementAgingRow
            {
                DocNo = o.OrderNo,
                Date = o.OrderDate,
                DaysOld = (int)(today - o.OrderDate.Date).TotalDays,
                Status = o.Status.ToString(),
                Supplier = o.Supplier?.Name ?? "",
                LineCount = o.Lines.Count,
                Quantity = Math.Round(o.Lines.Sum(l => l.Quantity), 2)
            })
            .OrderByDescending(r => r.DaysOld)
            .ToList();

        return new ProcurementAgingViewModel
        {
            Requests = requestRows,
            Orders = orderRows,
            PendingRequestCount = requestRows.Count,
            OpenOrderCount = orderRows.Count
        };
    }

    public async Task<TopSuppliersViewModel> GetTopSuppliersAsync(int companyId, DateTime? fromDate, DateTime? toDate, int top = 10)
    {
        var from = fromDate?.Date ?? DateTime.MinValue.Date;
        var to = toDate?.Date ?? DateTime.MaxValue.Date;

        var invoices = await _db.PurchaseInvoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Supplier)
            .Where(i => i.CompanyId == companyId && i.InvoiceDate >= from && i.InvoiceDate <= to)
            .ToListAsync();

        var grouped = invoices
            .GroupBy(i => i.SupplierId)
            .Select(g => new
            {
                Supplier = g.First().Supplier?.Name ?? "Unknown",
                InvoiceCount = g.Count(),
                Quantity = Math.Round(g.Sum(i => i.Lines.Sum(l => l.Quantity)), 2),
                Amount = Math.Round(g.Sum(i => i.Lines.Sum(l => l.Quantity * l.UnitPrice)), 2)
            })
            .OrderByDescending(x => x.Amount)
            .Take(top)
            .ToList();

        var totalAmount = grouped.Sum(x => x.Amount);
        var rows = grouped
            .Select((x, idx) => new TopSupplierRow
            {
                Rank = idx + 1,
                Supplier = x.Supplier,
                InvoiceCount = x.InvoiceCount,
                Quantity = x.Quantity,
                Amount = x.Amount,
                Percent = totalAmount == 0 ? 0 : Math.Round(x.Amount / totalAmount * 100m, 2)
            })
            .ToList();

        return new TopSuppliersViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            Top = top,
            Rows = rows,
            TotalPurchases = totalAmount
        };
    }
}