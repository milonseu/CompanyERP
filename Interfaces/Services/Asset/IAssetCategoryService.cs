using CompanyERP.Entities.Asset;

namespace CompanyERP.Interfaces.Services;

public interface IAssetCategoryService
{
    Task<List<AssetCategory>> GetAllAsync(int companyId);
    Task<AssetCategory?> GetByIdAsync(int id);
    Task<(bool Success, string Error)> CreateAsync(AssetCategory category);
    Task<(bool Success, string Error)> UpdateAsync(AssetCategory category);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}