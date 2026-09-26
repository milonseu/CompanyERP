using CompanyERP.Data;
using CompanyERP.Entities.Payment;
using CompanyERP.Interfaces.Services;
using CompanyERP.ViewModels.Reports;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Reports;

public class PaymentReportService : IPaymentReportService
{
    private readonly ApplicationDbContext _db;

    public PaymentReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PaymentSummaryViewModel> GetSummaryAsync(int companyId, DateTime? fromDate, DateTime? toDate, int? branchId)
    {
        var from = fromDate?.Date ?? DateTime.MinValue.Date;
        var to = toDate?.Date ?? DateTime.MaxValue.Date;

        var payments = await _db.Payments
            .AsNoTracking()
            .Include(p => p.PaymentMethod)
            .Include(p => p.CashAccount)
            .Include(p => p.BankAccount)
            .Where(p => p.CompanyId == companyId && p.PaymentDate >= from && p.PaymentDate <= to)
            .ToListAsync();

        if (branchId.HasValue)
        {
            payments = payments.Where(p => p.BranchId == branchId.Value).ToList();
        }

        var methods = payments
            .GroupBy(p => p.PaymentMethod?.Name ?? "Unknown")
            .Select(g => new PaymentMethodRow { Name = g.Key, Count = g.Count(), Amount = Math.Round(g.Sum(p => p.Amount), 2) })
            .OrderByDescending(g => g.Amount)
            .ToList();

        var categories = payments
            .GroupBy(p => p.Category.ToString())
            .Select(g => new PaymentCategoryRow { Category = g.Key, Count = g.Count(), Amount = Math.Round(g.Sum(p => p.Amount), 2) })
            .OrderByDescending(g => g.Amount)
            .ToList();

        var accounts = payments
            .GroupBy(p => p.AccountType == PaymentAccountType.Cash
                ? $"Cash - {p.CashAccount?.AccountName ?? "Unknown"}"
                : $"Bank - {p.BankAccount?.AccountName ?? "Unknown"}")
            .Select(g => new PaymentAccountRow { Name = g.Key, Amount = Math.Round(g.Sum(p => p.Amount), 2) })
            .OrderByDescending(g => g.Amount)
            .ToList();

        return new PaymentSummaryViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            BranchId = branchId,
            Methods = methods,
            Categories = categories,
            Accounts = accounts,
            TotalAmount = Math.Round(payments.Sum(p => p.Amount), 2)
        };
    }

    public async Task<SalesVsPaymentViewModel> GetSalesVsPaymentAsync(int companyId, DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate?.Date ?? DateTime.MinValue.Date;
        var to = toDate?.Date ?? DateTime.MaxValue.Date;

        var invoices = await _db.SalesInvoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Customer)
            .Where(i => i.CompanyId == companyId && i.InvoiceDate >= from && i.InvoiceDate <= to)
            .ToListAsync();

        var payments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId && p.Category == PaymentCategory.Customer && p.PaymentDate >= from && p.PaymentDate <= to)
            .ToListAsync();

        var byCustomer = new SortedDictionary<int, SalesVsPaymentRow>(Comparer<int>.Default);

        foreach (var inv in invoices)
        {
            var key = inv.CustomerId;
            if (!byCustomer.TryGetValue(key, out var row))
            {
                row = new SalesVsPaymentRow
                {
                    CustomerId = key,
                    Customer = inv.Customer?.Name ?? "Unknown",
                    Sales = 0,
                    Paid = 0
                };
                byCustomer[key] = row;
            }

            row.Sales = Math.Round(row.Sales + inv.Lines.Sum(l => l.Quantity * l.UnitPrice), 2);
        }

        foreach (var pay in payments)
        {
            if (!pay.CustomerId.HasValue)
            {
                continue;
            }

            var key = pay.CustomerId.Value;
            if (!byCustomer.TryGetValue(key, out var row))
            {
                row = new SalesVsPaymentRow
                {
                    CustomerId = key,
                    Customer = pay.Customer?.Name ?? "Unknown",
                    Sales = 0,
                    Paid = 0
                };
                byCustomer[key] = row;
            }

            row.Paid = Math.Round(row.Paid + pay.Amount, 2);
        }

        var rows = byCustomer.Values
            .Select(r =>
            {
                r.Balance = Math.Round(r.Sales - r.Paid, 2);
                return r;
            })
            .OrderBy(r => r.Customer)
            .ToList();

        return new SalesVsPaymentViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            Rows = rows,
            TotalSales = Math.Round(rows.Sum(r => r.Sales), 2),
            TotalPaid = Math.Round(rows.Sum(r => r.Paid), 2),
            TotalBalance = Math.Round(rows.Sum(r => r.Balance), 2)
        };
    }

    public async Task<PurchasesVsPaymentViewModel> GetPurchasesVsPaymentAsync(int companyId, DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate?.Date ?? DateTime.MinValue.Date;
        var to = toDate?.Date ?? DateTime.MaxValue.Date;

        var invoices = await _db.PurchaseInvoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Supplier)
            .Where(i => i.CompanyId == companyId && i.InvoiceDate >= from && i.InvoiceDate <= to)
            .ToListAsync();

        var payments = await _db.Payments
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Where(p => p.CompanyId == companyId && p.Category == PaymentCategory.Supplier && p.PaymentDate >= from && p.PaymentDate <= to)
            .ToListAsync();

        var bySupplier = new SortedDictionary<int, PurchasesVsPaymentRow>(Comparer<int>.Default);

        foreach (var inv in invoices)
        {
            var key = inv.SupplierId;
            if (!bySupplier.TryGetValue(key, out var row))
            {
                row = new PurchasesVsPaymentRow
                {
                    SupplierId = key,
                    Supplier = inv.Supplier?.Name ?? "Unknown",
                    Purchases = 0,
                    Paid = 0
                };
                bySupplier[key] = row;
            }

            row.Purchases = Math.Round(row.Purchases + inv.Lines.Sum(l => l.Quantity * l.UnitPrice), 2);
        }

        foreach (var pay in payments)
        {
            if (!pay.SupplierId.HasValue)
            {
                continue;
            }

            var key = pay.SupplierId.Value;
            if (!bySupplier.TryGetValue(key, out var row))
            {
                row = new PurchasesVsPaymentRow
                {
                    SupplierId = key,
                    Supplier = pay.Supplier?.Name ?? "Unknown",
                    Purchases = 0,
                    Paid = 0
                };
                bySupplier[key] = row;
            }

            row.Paid = Math.Round(row.Paid + pay.Amount, 2);
        }

        var rows = bySupplier.Values
            .Select(r =>
            {
                r.Balance = Math.Round(r.Purchases - r.Paid, 2);
                return r;
            })
            .OrderBy(r => r.Supplier)
            .ToList();

        return new PurchasesVsPaymentViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            Rows = rows,
            TotalPurchases = Math.Round(rows.Sum(r => r.Purchases), 2),
            TotalPaid = Math.Round(rows.Sum(r => r.Paid), 2),
            TotalBalance = Math.Round(rows.Sum(r => r.Balance), 2)
        };
    }
}