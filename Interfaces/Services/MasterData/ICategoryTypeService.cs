using CompanyERP.Entities.MasterData;

namespace CompanyERP.Interfaces.Services;

public interface ICategoryTypeService
{
    Task<List<CategoryType>> GetAllAsync();
    Task<List<CategoryType>> GetByCompanyIdAsync(int companyId);
    Task<CategoryType?> GetByIdAsync(int id);
    Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId = null);
    Task<bool> HasCategoriesAsync(int id);
    Task<(bool Success, string Error)> CreateAsync(CategoryType categoryType);
    Task<(bool Success, string Error)> UpdateAsync(CategoryType categoryType);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}