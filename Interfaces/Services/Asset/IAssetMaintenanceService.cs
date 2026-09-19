using CompanyERP.Entities.Asset;

namespace CompanyERP.Interfaces.Services;

public interface IAssetMaintenanceService
{
    Task<List<AssetMaintenance>> GetAllAsync(int companyId, int? assetId = null);
    Task<(bool Success, string Error)> CreateAsync(AssetMaintenance maintenance);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}