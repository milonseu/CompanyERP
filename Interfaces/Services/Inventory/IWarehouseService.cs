using CompanyERP.Entities.Inventory;

namespace CompanyERP.Interfaces.Services;

public interface IWarehouseService
{
    Task<List<Warehouse>> GetAllAsync();
    Task<List<Warehouse>> GetByCompanyIdAsync(int companyId);
    Task<Warehouse?> GetByIdAsync(int id);
    Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId = null);
    Task<bool> HasStockAsync(int id);
    Task<(bool Success, string Error)> CreateAsync(Warehouse warehouse);
    Task<(bool Success, string Error)> UpdateAsync(Warehouse warehouse);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}