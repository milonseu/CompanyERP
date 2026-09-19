using CompanyERP.Data;
using CompanyERP.Entities.Asset;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Asset;

public class AssetMaintenanceService : IAssetMaintenanceService
{
    private readonly ApplicationDbContext _db;

    public AssetMaintenanceService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<AssetMaintenance>> GetAllAsync(int companyId, int? assetId = null)
    {
        var query = _db.AssetMaintenances
            .AsNoTracking()
            .Where(m => m.AssetRegister!.CompanyId == companyId)
            .Include(m => m.AssetRegister)
            .AsQueryable();

        if (assetId.HasValue)
        {
            query = query.Where(m => m.AssetRegisterId == assetId.Value);
        }

        return await query
            .OrderByDescending(m => m.MaintenanceDate)
            .ThenByDescending(m => m.Id)
            .ToListAsync();
    }

    public async Task<(bool Success, string Error)> CreateAsync(AssetMaintenance maintenance)
    {
        var asset = await _db.AssetRegisters.FindAsync(maintenance.AssetRegisterId);
        if (asset is null)
        {
            return (false, "Asset not found.");
        }

        if (asset.Status == AssetStatus.Disposed)
        {
            return (false, "Maintenance cannot be recorded for a disposed asset.");
        }

        maintenance.Description = string.IsNullOrWhiteSpace(maintenance.Description) ? string.Empty : maintenance.Description.Trim();
        maintenance.Vendor = string.IsNullOrWhiteSpace(maintenance.Vendor) ? null : maintenance.Vendor.Trim();

        if (string.IsNullOrWhiteSpace(maintenance.Description))
        {
            return (false, "Maintenance description is required.");
        }

        if (maintenance.Cost < 0)
        {
            return (false, "Maintenance cost cannot be negative.");
        }

        maintenance.MaintenanceDate = maintenance.MaintenanceDate == default ? DateTime.Today : maintenance.MaintenanceDate;
        _db.AssetMaintenances.Add(maintenance);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var maintenance = await _db.AssetMaintenances.FindAsync(id);
        if (maintenance is null)
        {
            return (false, "Maintenance record not found.");
        }

        _db.AssetMaintenances.Remove(maintenance);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}