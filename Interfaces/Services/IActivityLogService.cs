using CompanyERP.Entities.Security;

namespace CompanyERP.Interfaces.Services;

public interface IActivityLogService
{
    Task<List<ActivityLog>> GetRecentAsync(int count = 100);
    Task<List<ActivityLog>> GetByModuleAsync(string module, int count = 100);
    Task LogAsync(string userName, string action, string? module, string? entityName, int? entityId, string details, string? ip);
    Task<List<string>> GetModulesAsync();
}