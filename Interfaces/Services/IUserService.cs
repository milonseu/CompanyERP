using CompanyERP.Entities.Security;

namespace CompanyERP.Interfaces.Services;

public interface IUserService
{
    Task<List<User>> GetAllAsync();
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUserNameAsync(string userName);
    Task<(bool Success, string Error)> CreateAsync(User user, string password);
    Task<(bool Success, string Error)> UpdateAsync(User user);
    Task<(bool Success, string Error)> DeleteAsync(int id);
    Task<(bool Success, string Error)> ChangePasswordAsync(int id, string newPassword);
    Task<List<Role>> GetUserRolesAsync(int userId);
    Task SetUserRolesAsync(int userId, List<int> roleIds);
    Task<List<int>> GetAssignedRoleIdsAsync(int userId);
    Task<List<string>> GetUserPermissionCodesAsync(int userId);
    Task SetUserPermissionsAsync(int userId, List<int> permissionIds);
    Task<List<int>> GetAssignedPermissionIdsAsync(int userId);
    Task<bool> UserNameExistsAsync(string userName, int? excludeId = null);
}