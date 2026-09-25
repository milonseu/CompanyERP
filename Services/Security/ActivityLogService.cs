using CompanyERP.Data;
using CompanyERP.Entities.Security;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Security;

public class ActivityLogService : IActivityLogService
{
    private readonly ApplicationDbContext _db;

    public ActivityLogService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<ActivityLog>> GetRecentAsync(int count = 100)
    {
        return await _db.ActivityLogs
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<ActivityLog>> GetByModuleAsync(string module, int count = 100)
    {
        return await _db.ActivityLogs
            .Where(a => a.Module == module)
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task LogAsync(string userName, string action, string? module, string? entityName, int? entityId, string details, string? ip)
    {
        _db.ActivityLogs.Add(new ActivityLog
        {
            UserName = string.IsNullOrWhiteSpace(userName) ? "System" : userName,
            Action = action,
            Module = module,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            IpAddress = ip
        });
        await _db.SaveChangesAsync();
    }

    public async Task<List<string>> GetModulesAsync()
    {
        return await _db.ActivityLogs
            .Where(a => a.Module != null)
            .Select(a => a.Module!)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();
    }
}