using CompanyERP.Entities.Inventory;

namespace CompanyERP.Interfaces.Services;

public interface IProductService
{
    Task<List<Product>> GetAllAsync();
    Task<List<Product>> GetByCompanyIdAsync(int companyId);
    Task<Product?> GetByIdAsync(int id);
    Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId = null);
    Task<bool> HasStockAsync(int id);
    Task<(bool Success, string Error)> CreateAsync(Product product);
    Task<(bool Success, string Error)> UpdateAsync(Product product);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}