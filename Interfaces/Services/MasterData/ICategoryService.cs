using CompanyERP.Entities.MasterData;

namespace CompanyERP.Interfaces.Services;

public interface ICategoryService
{
    Task<List<Category>> GetAllAsync();
    Task<List<Category>> GetByCompanyIdAsync(int companyId);
    Task<List<Category>> GetByCompanyAndTypeAsync(int companyId, int categoryTypeId);
    Task<Category?> GetByIdAsync(int id);
    Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId = null);
    Task<(bool Success, string Error)> CreateAsync(Category category);
    Task<(bool Success, string Error)> UpdateAsync(Category category);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}