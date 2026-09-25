using CompanyERP.Entities.Security;

namespace CompanyERP.Interfaces.Services;

public interface IRoleService
{
    Task<List<Role>> GetAllAsync();
    Task<Role?> GetByIdAsync(int id);
    Task<Role?> GetByCodeAsync(string code);
    Task<(bool Success, string Error)> CreateAsync(Role role);
    Task<(bool Success, string Error)> UpdateAsync(Role role);
    Task<(bool Success, string Error)> DeleteAsync(int id);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    Task<bool> HasUsersAsync(int id);
    Task<List<Permission>> GetRolePermissionsAsync(int roleId);
    Task SetRolePermissionsAsync(int roleId, List<int> permissionIds);
    Task<List<int>> GetAssignedPermissionIdsAsync(int roleId);
}