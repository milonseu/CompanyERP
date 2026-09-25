using CompanyERP.Data;
using CompanyERP.Entities.Security;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Security;

public class LoginHistoryService : ILoginHistoryService
{
    private readonly ApplicationDbContext _db;

    public LoginHistoryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<LoginHistory>> GetRecentAsync(int count = 100)
    {
        return await _db.LoginHistories
            .OrderByDescending(h => h.LoginTime)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<LoginHistory>> GetByUserAsync(string userName, int count = 100)
    {
        return await _db.LoginHistories
            .Where(h => h.UserName == userName)
            .OrderByDescending(h => h.LoginTime)
            .Take(count)
            .ToListAsync();
    }

    public async Task LogAsync(string userName, DateTime loginTime, bool isSuccess, string? ip, string? failReason = null)
    {
        _db.LoginHistories.Add(new LoginHistory
        {
            UserName = userName,
            LoginTime = loginTime,
            IsSuccess = isSuccess,
            IpAddress = ip,
            FailReason = failReason
        });
        await _db.SaveChangesAsync();
    }

    public async Task MarkLogoutAsync(int id, string? ip)
    {
        var history = await _db.LoginHistories.FindAsync(id);
        if (history is not null)
        {
            history.LogoutTime = DateTime.Now;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<LoginHistory?> GetLastForUserAsync(string userName)
    {
        return await _db.LoginHistories
            .Where(h => h.UserName == userName)
            .OrderByDescending(h => h.LoginTime)
            .FirstOrDefaultAsync();
    }
}