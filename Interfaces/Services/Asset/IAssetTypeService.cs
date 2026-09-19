using CompanyERP.Entities.Asset;

namespace CompanyERP.Interfaces.Services;

public interface IAssetTypeService
{
    Task<List<AssetType>> GetAllAsync(int companyId);
    Task<AssetType?> GetByIdAsync(int id);
    Task<(bool Success, string Error)> CreateAsync(AssetType type);
    Task<(bool Success, string Error)> UpdateAsync(AssetType type);
    Task<(bool Success, string Error)> DeleteAsync(int id);
    Task<List<AssetType>> GetByCategoryAsync(int companyId, int categoryId);
}