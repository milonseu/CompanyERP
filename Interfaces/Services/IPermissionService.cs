using CompanyERP.Entities.Security;

namespace CompanyERP.Interfaces.Services;

public interface IPermissionService
{
    Task<List<Permission>> GetAllAsync();
    Task<List<string>> GetModulesAsync();
    Task<List<Permission>> GetByModuleAsync(string? module);
    Task<Permission?> GetByIdAsync(int id);
    Task<(bool Success, string Error)> CreateAsync(Permission permission);
    Task<(bool Success, string Error)> UpdateAsync(Permission permission);
    Task<(bool Success, string Error)> DeleteAsync(int id);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    Task<List<string>> GetAssignedPermissionCodesAsync(int userId);
}