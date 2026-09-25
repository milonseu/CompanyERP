using CompanyERP.Entities.Security;

namespace CompanyERP.Interfaces.Services;

public interface IMenuService
{
    Task<List<Menu>> GetAllAsync();
    Task<Menu?> GetByIdAsync(int id);
    Task<(bool Success, string Error)> CreateAsync(Menu menu);
    Task<(bool Success, string Error)> UpdateAsync(Menu menu);
    Task<(bool Success, string Error)> DeleteAsync(int id);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    Task<List<Menu>> GetTopLevelAsync();
    Task<List<Menu>> GetSubMenusAsync(int parentId);
    Task<bool> HasChildrenAsync(int id);
}