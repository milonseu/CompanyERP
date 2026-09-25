using CompanyERP.Data;
using CompanyERP.Entities.Security;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Security;

public class RoleService : IRoleService
{
    private readonly ApplicationDbContext _db;

    public RoleService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Role>> GetAllAsync()
    {
        return await _db.Roles
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<Role?> GetByIdAsync(int id)
    {
        return await _db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Role?> GetByCodeAsync(string code)
    {
        return await _db.Roles.FirstOrDefaultAsync(r => r.Code == code);
    }

    public async Task<(bool Success, string Error)> CreateAsync(Role role)
    {
        role.Code = string.IsNullOrWhiteSpace(role.Code) ? string.Empty : role.Code.Trim().ToUpperInvariant();
        role.Name = string.IsNullOrWhiteSpace(role.Name) ? string.Empty : role.Name.Trim();

        if (string.IsNullOrWhiteSpace(role.Code) || string.IsNullOrWhiteSpace(role.Name))
        {
            return (false, "Role code and name are required.");
        }

        if (await CodeExistsAsync(role.Code))
        {
            return (false, "Role code already exists.");
        }

        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(Role role)
    {
        var existing = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == role.Id);
        if (existing is null)
        {
            return (false, "Role not found.");
        }

        role.Code = string.IsNullOrWhiteSpace(role.Code) ? string.Empty : role.Code.Trim().ToUpperInvariant();
        if (await CodeExistsAsync(role.Code, role.Id))
        {
            return (false, "Role code already exists.");
        }

        role.IsSystem = existing.IsSystem;
        role.CreatedAt = existing.CreatedAt;
        role.CreatedBy = existing.CreatedBy;

        _db.Roles.Update(role);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var role = await _db.Roles.FindAsync(id);
        if (role is null)
        {
            return (false, "Role not found.");
        }

        if (role.IsSystem)
        {
            return (false, "System roles cannot be deleted.");
        }

        if (await HasUsersAsync(id))
        {
            return (false, "Role cannot be deleted because users are assigned to it.");
        }

        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
    {
        var query = _db.Roles.AsNoTracking().Where(r => r.Code == code);
        if (excludeId.HasValue)
        {
            query = query.Where(r => r.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<bool> HasUsersAsync(int id)
    {
        return await _db.UserRoles.AnyAsync(ur => ur.RoleId == id);
    }

    public async Task<List<Permission>> GetRolePermissionsAsync(int roleId)
    {
        return await (from rp in _db.RolePermissions
                      join p in _db.Permissions on rp.PermissionId equals p.Id
                      where rp.RoleId == roleId
                      select p)
            .OrderBy(p => p.Module).ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task SetRolePermissionsAsync(int roleId, List<int> permissionIds)
    {
        var existing = await _db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
        _db.RolePermissions.RemoveRange(existing);

        foreach (var permissionId in permissionIds.Distinct())
        {
            if (await _db.Permissions.AnyAsync(p => p.Id == permissionId))
            {
                _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<int>> GetAssignedPermissionIdsAsync(int roleId)
    {
        return await _db.RolePermissions.Where(rp => rp.RoleId == roleId).Select(rp => rp.PermissionId).ToListAsync();
    }
}