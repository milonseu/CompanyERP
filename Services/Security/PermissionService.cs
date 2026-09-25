using CompanyERP.Data;
using CompanyERP.Entities.Security;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Security;

public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuthService _authService;

    public PermissionService(ApplicationDbContext db, IAuthService authService)
    {
        _db = db;
        _authService = authService;
    }

    public async Task<List<Permission>> GetAllAsync()
    {
        return await _db.Permissions
            .OrderBy(p => p.Module).ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<List<string>> GetModulesAsync()
    {
        return await _db.Permissions
            .Where(p => p.Module != null)
            .Select(p => p.Module!)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();
    }

    public async Task<List<Permission>> GetByModuleAsync(string? module)
    {
        var query = _db.Permissions.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(p => p.Module == module);
        }
        return await query.OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<Permission?> GetByIdAsync(int id)
    {
        return await _db.Permissions.FindAsync(id);
    }

    public async Task<(bool Success, string Error)> CreateAsync(Permission permission)
    {
        permission.Code = string.IsNullOrWhiteSpace(permission.Code) ? string.Empty : permission.Code.Trim().ToUpperInvariant();
        permission.Name = string.IsNullOrWhiteSpace(permission.Name) ? string.Empty : permission.Name.Trim();

        if (string.IsNullOrWhiteSpace(permission.Code) || string.IsNullOrWhiteSpace(permission.Name))
        {
            return (false, "Permission code and name are required.");
        }

        if (await CodeExistsAsync(permission.Code))
        {
            return (false, "Permission code already exists.");
        }

        _db.Permissions.Add(permission);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(Permission permission)
    {
        var existing = await _db.Permissions.AsNoTracking().FirstOrDefaultAsync(p => p.Id == permission.Id);
        if (existing is null)
        {
            return (false, "Permission not found.");
        }

        permission.IsSystem = existing.IsSystem;
        permission.CreatedAt = existing.CreatedAt;
        permission.CreatedBy = existing.CreatedBy;

        _db.Permissions.Update(permission);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var permission = await _db.Permissions.FindAsync(id);
        if (permission is null)
        {
            return (false, "Permission not found.");
        }

        if (permission.IsSystem)
        {
            return (false, "System permissions cannot be deleted.");
        }

        _db.Permissions.Remove(permission);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
    {
        var query = _db.Permissions.AsNoTracking().Where(p => p.Code == code);
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<List<string>> GetAssignedPermissionCodesAsync(int userId)
    {
        return await _authService.GetPermissionCodesAsync(userId);
    }
}