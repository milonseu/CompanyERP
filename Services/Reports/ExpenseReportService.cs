using CompanyERP.Data;
using CompanyERP.Entities.Expense;
using CompanyERP.Interfaces.Services;
using CompanyERP.ViewModels.Reports;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Reports;

public class ExpenseReportService : IExpenseReportService
{
    private readonly ApplicationDbContext _db;

    public ExpenseReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ExpenseSummaryViewModel> GetSummaryAsync(int companyId, DateTime? fromDate, DateTime? toDate, int? expenseTypeId)
    {
        var from = fromDate?.Date ?? DateTime.MinValue.Date;
        var to = toDate?.Date ?? DateTime.MaxValue.Date;

        var entries = await _db.ExpenseEntries
            .AsNoTracking()
            .Include(e => e.ExpenseType)
            .Where(e => e.CompanyId == companyId && e.ExpenseDate >= from && e.ExpenseDate <= to)
            .ToListAsync();

        var rows = entries
            .Where(e => !expenseTypeId.HasValue || e.ExpenseTypeId == expenseTypeId.Value)
            .GroupBy(e => e.ExpenseType?.Name ?? "Unknown")
            .Select(g => new ExpenseRow
            {
                Type = g.Key,
                Count = g.Count(),
                Amount = Math.Round(g.Sum(e => e.Amount), 2),
                Paid = Math.Round(g.Sum(e => Math.Min(e.AmountPaid, e.Amount)), 2),
                Outstanding = Math.Round(g.Sum(e => Math.Max(0, e.Amount - e.AmountPaid)), 2)
            })
            .OrderByDescending(r => r.Amount)
            .ToList();

        return new ExpenseSummaryViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            ExpenseTypeId = expenseTypeId,
            Rows = rows,
            TotalAmount = Math.Round(rows.Sum(r => r.Amount), 2),
            TotalPaid = Math.Round(rows.Sum(r => r.Paid), 2),
            TotalOutstanding = Math.Round(rows.Sum(r => r.Outstanding), 2)
        };
    }

    public async Task<ExpenseTrendViewModel> GetTrendAsync(int companyId)
    {
        var entries = await _db.ExpenseEntries
            .AsNoTracking()
            .Where(e => e.CompanyId == companyId)
            .ToListAsync();

        var rows = entries
            .GroupBy(e => e.ExpenseDate.ToString("yyyy-MM"))
            .Select(g => new ExpenseTrendRow
            {
                Month = g.Key,
                Count = g.Count(),
                Amount = Math.Round(g.Sum(e => e.Amount), 2),
                Paid = Math.Round(g.Sum(e => Math.Min(e.AmountPaid, e.Amount)), 2),
                Outstanding = Math.Round(g.Sum(e => Math.Max(0, e.Amount - e.AmountPaid)), 2)
            })
            .OrderBy(r => r.Month)
            .ToList();

        return new ExpenseTrendViewModel
        {
            Rows = rows,
            TotalAmount = Math.Round(rows.Sum(r => r.Amount), 2)
        };
    }

    public async Task<ExpenseRegisterViewModel> GetRegisterAsync(int companyId, DateTime? fromDate, DateTime? toDate, int? branchId, int? supplierId)
    {
        var from = fromDate?.Date ?? DateTime.MinValue.Date;
        var to = toDate?.Date ?? DateTime.MaxValue.Date;

        var entries = await _db.ExpenseEntries
            .AsNoTracking()
            .Include(e => e.ExpenseType)
            .Include(e => e.Supplier)
            .Include(e => e.Branch)
            .Where(e => e.CompanyId == companyId && e.ExpenseDate >= from && e.ExpenseDate <= to)
            .ToListAsync();

        var rows = entries
            .Where(e => (!branchId.HasValue || e.BranchId == branchId.Value) &&
                        (!supplierId.HasValue || e.SupplierId == supplierId.Value))
            .Select(e => new ExpenseRegisterRow
            {
                Date = e.ExpenseDate,
                ExpenseNo = e.ExpenseNo,
                Type = e.ExpenseType?.Name ?? "",
                Supplier = e.Supplier?.Name ?? "",
                Branch = e.Branch?.Name ?? "",
                Description = e.Description,
                Amount = Math.Round(e.Amount, 2),
                Paid = Math.Round(Math.Min(e.AmountPaid, e.Amount), 2),
                Outstanding = Math.Round(Math.Max(0, e.Amount - e.AmountPaid), 2)
            })
            .OrderByDescending(r => r.Date)
            .ThenBy(r => r.ExpenseNo)
            .ToList();

        return new ExpenseRegisterViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            BranchId = branchId,
            SupplierId = supplierId,
            Rows = rows,
            TotalAmount = Math.Round(rows.Sum(r => r.Amount), 2),
            TotalPaid = Math.Round(rows.Sum(r => r.Paid), 2),
            TotalOutstanding = Math.Round(rows.Sum(r => r.Outstanding), 2)
        };
    }
}