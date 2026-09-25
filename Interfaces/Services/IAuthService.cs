using CompanyERP.Entities.Security;

namespace CompanyERP.Interfaces.Services;

public interface IAuthService
{
    Task<(bool Success, string Error, User? User)> ValidateAsync(string userName, string password);
    Task<User?> GetByUserNameAsync(string userName);
    Task<List<string>> GetPermissionCodesAsync(int userId);
    Task<List<string>> GetRoleCodesAsync(int userId);
    Task<bool> HasPermissionAsync(int userId, string permissionCode);
    Task<bool> IsSuperAdminAsync(int userId);
    Task<bool> UserNameExistsAsync(string userName, int? excludeId = null);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    Task RecordLoginSuccessAsync(int userId, string? ip);
    Task RecordLoginFailAsync(string userName, string? ip, string failReason);
    Task RecordLogoutAsync(int userId);
    Task LogAsync(string userName, string action, string? module, string? entityName, int? entityId, string details, string? ip);
}