using CompanyERP.Data;
using CompanyERP.Entities.Asset;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Asset;

public class AssetDepreciationService : IAssetDepreciationService
{
    private readonly ApplicationDbContext _db;

    public AssetDepreciationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<AssetDepreciation>> GetAllAsync(int companyId, string? periodKey = null)
    {
        var query = _db.AssetDepreciations
            .AsNoTracking()
            .Where(d => d.AssetRegister!.CompanyId == companyId)
            .Include(d => d.AssetRegister)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(periodKey))
        {
            query = query.Where(d => d.PeriodKey == periodKey.Trim());
        }

        return await query
            .OrderByDescending(d => d.PeriodDate)
            .ThenBy(d => d.AssetRegister!.AssetNo)
            .ToListAsync();
    }

    public async Task<List<string>> GetPeriodsAsync(int companyId)
    {
        return await _db.AssetDepreciations
            .AsNoTracking()
            .Where(d => d.AssetRegister!.CompanyId == companyId)
            .Select(d => d.PeriodKey)
            .Distinct()
            .OrderByDescending(p => p)
            .ToListAsync();
    }

    public async Task<(bool Success, string Error, int Count, decimal Total)> RunAsync(int companyId, string periodKey, string? note = null)
    {
        periodKey = string.IsNullOrWhiteSpace(periodKey) ? string.Empty : periodKey.Trim();
        if (!IsValidPeriod(periodKey))
        {
            return (false, "Period must be in yyyy-MM format.", 0, 0);
        }

        var periodDate = LastDayOfMonth(periodKey);
        var existing = await _db.AssetDepreciations
            .AnyAsync(d => d.PeriodKey == periodKey && d.AssetRegister!.CompanyId == companyId);
        if (existing)
        {
            return (false, $"Depreciation for period {periodKey} has already been posted for this company.", 0, 0);
        }

        var assets = await _db.AssetRegisters
            .Where(a => a.CompanyId == companyId && a.Status != AssetStatus.Disposed)
            .ToListAsync();

        int count = 0;
        decimal total = 0;
        foreach (var asset in assets)
        {
            var amount = CalculateAmount(asset);
            if (amount <= 0)
            {
                continue;
            }

            var accumulatedAfter = Math.Round(asset.AccumulatedDepreciation + amount, 2);
            _db.AssetDepreciations.Add(new AssetDepreciation
            {
                AssetRegisterId = asset.Id,
                PeriodKey = periodKey,
                PeriodDate = periodDate,
                Amount = Math.Round(amount, 2),
                AccumulatedAfter = accumulatedAfter,
                Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
            });
            asset.AccumulatedDepreciation = accumulatedAfter;
            _db.AssetRegisters.Update(asset);
            count++;
            total += amount;
        }

        await _db.SaveChangesAsync();
        return (true, string.Empty, count, Math.Round(total, 2));
    }

    public async Task<(bool Success, string Error, int Count, decimal Total)> RunForAssetAsync(int assetId, string periodKey, string? note = null)
    {
        periodKey = string.IsNullOrWhiteSpace(periodKey) ? string.Empty : periodKey.Trim();
        if (!IsValidPeriod(periodKey))
        {
            return (false, "Period must be in yyyy-MM format.", 0, 0);
        }

        var asset = await _db.AssetRegisters.FindAsync(assetId);
        if (asset is null)
        {
            return (false, "Asset not found.", 0, 0);
        }

        if (asset.Status == AssetStatus.Disposed)
        {
            return (false, "Depreciation cannot be posted for a disposed asset.", 0, 0);
        }

        if (await _db.AssetDepreciations.AnyAsync(d => d.AssetRegisterId == assetId && d.PeriodKey == periodKey))
        {
            return (false, $"Depreciation for period {periodKey} already exists for this asset.", 0, 0);
        }

        var amount = CalculateAmount(asset);
        if (amount <= 0)
        {
            return (false, "Asset has reached its full depreciable amount.", 0, 0);
        }

        var accumulatedAfter = Math.Round(asset.AccumulatedDepreciation + amount, 2);
        _db.AssetDepreciations.Add(new AssetDepreciation
        {
            AssetRegisterId = assetId,
            PeriodKey = periodKey,
            PeriodDate = LastDayOfMonth(periodKey),
            Amount = Math.Round(amount, 2),
            AccumulatedAfter = accumulatedAfter,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        });
        asset.AccumulatedDepreciation = accumulatedAfter;
        _db.AssetRegisters.Update(asset);
        await _db.SaveChangesAsync();
        return (true, string.Empty, 1, Math.Round(amount, 2));
    }

    private static bool IsValidPeriod(string periodKey)
    {
        return DateTime.TryParseExact(periodKey, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out _);
    }

    private static DateTime LastDayOfMonth(string periodKey)
    {
        var parsed = DateTime.ParseExact(periodKey, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
        return new DateTime(parsed.Year, parsed.Month, DateTime.DaysInMonth(parsed.Year, parsed.Month));
    }

    private static decimal CalculateAmount(AssetRegister asset)
    {
        if (asset.UsefulLifeMonths <= 0 || asset.Cost <= 0)
        {
            return 0;
        }

        var remaining = asset.DepreciableAmount - asset.AccumulatedDepreciation;
        if (remaining <= 0)
        {
            return 0;
        }

        decimal amount;
        if (asset.DepreciationMethod == AssetDepreciationMethod.ReducingBalance)
        {
            var factor = 2m / asset.UsefulLifeMonths;
            amount = (asset.Cost - asset.AccumulatedDepreciation) * factor;
        }
        else
        {
            amount = asset.DepreciableAmount / asset.UsefulLifeMonths;
        }

        amount = Math.Round(amount, 2);
        return Math.Min(amount, remaining);
    }
}
