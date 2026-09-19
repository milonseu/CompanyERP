using CompanyERP.Entities.Asset;

namespace CompanyERP.Interfaces.Services;

public interface IAssetDepreciationService
{
    Task<List<AssetDepreciation>> GetAllAsync(int companyId, string? periodKey = null);
    Task<List<string>> GetPeriodsAsync(int companyId);
    Task<(bool Success, string Error, int Count, decimal Total)> RunAsync(int companyId, string periodKey, string? note = null);
    Task<(bool Success, string Error, int Count, decimal Total)> RunForAssetAsync(int assetId, string periodKey, string? note = null);
}