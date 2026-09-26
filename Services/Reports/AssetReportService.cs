using CompanyERP.Data;
using CompanyERP.Entities.Asset;
using CompanyERP.Interfaces.Services;
using CompanyERP.ViewModels.Reports;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Reports;

public class AssetReportService : IAssetReportService
{
    private readonly ApplicationDbContext _db;

    public AssetReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AssetReportViewModel> GetRegisterAsync(int companyId, DateTime? asOfDate, int? assetTypeId)
    {
        var asOf = asOfDate?.Date ?? DateTime.MaxValue.Date;

        var assets = await _db.AssetRegisters
            .AsNoTracking()
            .Include(a => a.AssetType)
            .Include(a => a.Branch)
            .Where(a => a.CompanyId == companyId && a.PurchaseDate <= asOf)
            .ToListAsync();

        var rows = assets
            .Where(a => !assetTypeId.HasValue || a.AssetTypeId == assetTypeId.Value)
            .Select(a => new AssetRegisterRow
            {
                AssetNo = a.AssetNo,
                Name = a.Name,
                Type = a.AssetType?.Name ?? "",
                Branch = a.Branch?.Name ?? "",
                PurchaseDate = a.PurchaseDate,
                Cost = Math.Round(a.Cost, 2),
                AccumulatedDepreciation = Math.Round(a.AccumulatedDepreciation, 2),
                BookValue = Math.Round(Math.Max(0, a.Cost - a.AccumulatedDepreciation), 2),
                Status = a.Status.ToString()
            })
            .OrderBy(r => r.AssetNo)
            .ToList();

        return new AssetReportViewModel
        {
            AsOfDate = asOfDate,
            AssetTypeId = assetTypeId,
            Rows = rows,
            TotalCost = Math.Round(rows.Sum(r => r.Cost), 2),
            TotalAccumulatedDepreciation = Math.Round(rows.Sum(r => r.AccumulatedDepreciation), 2),
            TotalBookValue = Math.Round(rows.Sum(r => r.BookValue), 2)
        };
    }

    public async Task<DepreciationScheduleViewModel> GetDepreciationScheduleAsync(int companyId, string? fromPeriod, string? toPeriod)
    {
        var query = _db.AssetDepreciations
            .AsNoTracking()
            .Include(d => d.AssetRegister)
            .Where(d => d.AssetRegister != null && d.AssetRegister.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(fromPeriod))
        {
            query = query.Where(d => d.PeriodKey.CompareTo(fromPeriod) >= 0);
        }

        if (!string.IsNullOrWhiteSpace(toPeriod))
        {
            query = query.Where(d => d.PeriodKey.CompareTo(toPeriod) <= 0);
        }

        var rows = (await query
            .OrderBy(d => d.PeriodKey)
            .ThenBy(d => d.AssetRegister!.AssetNo)
            .ToListAsync())
            .Select(d => new DepreciationRow
            {
                AssetNo = d.AssetRegister!.AssetNo,
                Asset = d.AssetRegister.Name,
                PeriodKey = d.PeriodKey,
                PeriodDate = d.PeriodDate,
                Amount = Math.Round(d.Amount, 2),
                AccumulatedAfter = Math.Round(d.AccumulatedAfter, 2),
                MonthlyProjected = d.AssetRegister.UsefulLifeMonths > 0
                    ? Math.Round(Math.Max(0, d.AssetRegister.Cost - d.AssetRegister.SalvageValue) / d.AssetRegister.UsefulLifeMonths, 2)
                    : 0
            })
            .ToList();

        return new DepreciationScheduleViewModel
        {
            FromPeriod = fromPeriod,
            ToPeriod = toPeriod,
            Rows = rows,
            TotalDepreciation = Math.Round(rows.Sum(r => r.Amount), 2)
        };
    }

    public async Task<DisposalSummaryViewModel> GetDisposalsAsync(int companyId)
    {
        var rows = (await _db.AssetDisposals
            .AsNoTracking()
            .Include(d => d.AssetRegister)
            .Where(d => d.AssetRegister != null && d.AssetRegister.CompanyId == companyId)
            .OrderBy(d => d.DisposalDate)
            .ToListAsync())
            .Select(d => new DisposalRow
            {
                AssetNo = d.AssetRegister!.AssetNo,
                Asset = d.AssetRegister.Name,
                DisposalDate = d.DisposalDate,
                SaleValue = Math.Round(d.SaleValue, 2),
                BookValue = Math.Round(d.BookValueAtDisposal, 2),
                GainLoss = Math.Round(d.GainLossAmount, 2)
            })
            .ToList();

        return new DisposalSummaryViewModel
        {
            Rows = rows,
            TotalSaleValue = Math.Round(rows.Sum(r => r.SaleValue), 2),
            TotalBookValue = Math.Round(rows.Sum(r => r.BookValue), 2),
            TotalGainLoss = Math.Round(rows.Sum(r => r.GainLoss), 2)
        };
    }

    public async Task<AssetGroupSummaryViewModel> GetGroupSummaryAsync(int companyId)
    {
        var assets = await _db.AssetRegisters
            .AsNoTracking()
            .Include(a => a.AssetType)
            .Include(a => a.Branch)
            .Where(a => a.CompanyId == companyId)
            .ToListAsync();

        var byStatus = assets
            .GroupBy(a => a.Status.ToString())
            .Select(g => new AssetGroupRow
            {
                Label = g.Key,
                Count = g.Count(),
                Cost = Math.Round(g.Sum(a => a.Cost), 2),
                BookValue = Math.Round(g.Sum(a => Math.Max(0, a.Cost - a.AccumulatedDepreciation)), 2)
            })
            .OrderBy(r => r.Label)
            .ToList();

        var byType = assets
            .GroupBy(a => a.AssetType?.Name ?? "Unknown")
            .Select(g => new AssetGroupRow
            {
                Label = g.Key,
                Count = g.Count(),
                Cost = Math.Round(g.Sum(a => a.Cost), 2),
                BookValue = Math.Round(g.Sum(a => Math.Max(0, a.Cost - a.AccumulatedDepreciation)), 2)
            })
            .OrderByDescending(r => r.Cost)
            .ToList();

        var byBranch = assets
            .GroupBy(a => a.Branch?.Name ?? "Unknown")
            .Select(g => new AssetGroupRow
            {
                Label = g.Key,
                Count = g.Count(),
                Cost = Math.Round(g.Sum(a => a.Cost), 2),
                BookValue = Math.Round(g.Sum(a => Math.Max(0, a.Cost - a.AccumulatedDepreciation)), 2)
            })
            .OrderByDescending(r => r.Cost)
            .ToList();

        return new AssetGroupSummaryViewModel
        {
            ByStatus = byStatus,
            ByType = byType,
            ByBranch = byBranch,
            TotalCost = Math.Round(assets.Sum(a => a.Cost), 2),
            TotalBookValue = Math.Round(assets.Sum(a => Math.Max(0, a.Cost - a.AccumulatedDepreciation)), 2)
        };
    }
}