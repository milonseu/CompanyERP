using CompanyERP.Entities.Inventory;

namespace CompanyERP.Interfaces.Services;

public interface IProductCategoryService
{
    Task<List<ProductCategory>> GetAllAsync();
    Task<List<ProductCategory>> GetByCompanyIdAsync(int companyId);
    Task<ProductCategory?> GetByIdAsync(int id);
    Task<bool> NameExistsAsync(string name, int companyId, int? excludeId = null);
    Task<bool> HasProductsAsync(int id);
    Task<(bool Success, string Error)> CreateAsync(ProductCategory category);
    Task<(bool Success, string Error)> UpdateAsync(ProductCategory category);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}