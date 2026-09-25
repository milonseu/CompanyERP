using CompanyERP.Entities.Security;

namespace CompanyERP.Interfaces.Services;

public interface ILoginHistoryService
{
    Task<List<LoginHistory>> GetRecentAsync(int count = 100);
    Task<List<LoginHistory>> GetByUserAsync(string userName, int count = 100);
    Task LogAsync(string userName, DateTime loginTime, bool isSuccess, string? ip, string? failReason = null);
    Task MarkLogoutAsync(int id, string? ip);
    Task<LoginHistory?> GetLastForUserAsync(string userName);
}