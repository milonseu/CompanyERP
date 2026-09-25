using CompanyERP.Data;
using CompanyERP.Entities.Security;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Security;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuthService _authService;

    public UserService(ApplicationDbContext db, IAuthService authService)
    {
        _db = db;
        _authService = authService;
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderBy(u => u.UserName)
            .ToListAsync();
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public Task<User?> GetByUserNameAsync(string userName)
    {
        return _db.Users.FirstOrDefaultAsync(u => u.UserName == userName);
    }

    public async Task<(bool Success, string Error)> CreateAsync(User user, string password)
    {
        user.UserName = string.IsNullOrWhiteSpace(user.UserName) ? string.Empty : user.UserName.Trim();
        user.FullName = string.IsNullOrWhiteSpace(user.FullName) ? null : user.FullName.Trim();
        user.Email = string.IsNullOrWhiteSpace(user.Email) ? null : user.Email.Trim();

        if (string.IsNullOrWhiteSpace(user.UserName))
        {
            return (false, "User name is required.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return (false, "Password is required.");
        }

        if (await _authService.UserNameExistsAsync(user.UserName))
        {
            return (false, "User name already exists.");
        }

        user.PasswordHash = _authService.HashPassword(password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(User user)
    {
        var existing = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
        if (existing is null)
        {
            return (false, "User not found.");
        }

        user.UserName = string.IsNullOrWhiteSpace(user.UserName) ? string.Empty : user.UserName.Trim();
        if (await _authService.UserNameExistsAsync(user.UserName, user.Id))
        {
            return (false, "User name already exists.");
        }

        user.PasswordHash = existing.PasswordHash;
        user.IsSystem = existing.IsSystem;
        user.CreatedAt = existing.CreatedAt;
        user.CreatedBy = existing.CreatedBy;
        user.LastLoginAt = existing.LastLoginAt;

        _db.Users.Update(user);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null)
        {
            return (false, "User not found.");
        }

        if (user.IsSystem)
        {
            return (false, "System accounts cannot be deleted.");
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> ChangePasswordAsync(int id, string newPassword)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null)
        {
            return (false, "User not found.");
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            return (false, "New password is required.");
        }

        user.PasswordHash = _authService.HashPassword(newPassword);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<List<Role>> GetUserRolesAsync(int userId)
    {
        return await (from ur in _db.UserRoles
                      join r in _db.Roles on ur.RoleId equals r.Id
                      where ur.UserId == userId
                      select r)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task SetUserRolesAsync(int userId, List<int> roleIds)
    {
        var existing = await _db.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
        _db.UserRoles.RemoveRange(existing);

        foreach (var roleId in roleIds.Distinct())
        {
            if (await _db.Roles.AnyAsync(r => r.Id == roleId))
            {
                _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<int>> GetAssignedRoleIdsAsync(int userId)
    {
        return await _db.UserRoles.Where(ur => ur.UserId == userId).Select(ur => ur.RoleId).ToListAsync();
    }

    public async Task<List<string>> GetUserPermissionCodesAsync(int userId)
    {
        return await _authService.GetPermissionCodesAsync(userId);
    }

    public async Task SetUserPermissionsAsync(int userId, List<int> permissionIds)
    {
        var existing = await _db.UserPermissions.Where(up => up.UserId == userId).ToListAsync();
        _db.UserPermissions.RemoveRange(existing);

        foreach (var permissionId in permissionIds.Distinct())
        {
            if (await _db.Permissions.AnyAsync(p => p.Id == permissionId))
            {
                _db.UserPermissions.Add(new UserPermission { UserId = userId, PermissionId = permissionId });
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<int>> GetAssignedPermissionIdsAsync(int userId)
    {
        return await _db.UserPermissions.Where(up => up.UserId == userId).Select(up => up.PermissionId).ToListAsync();
    }

    public Task<bool> UserNameExistsAsync(string userName, int? excludeId = null)
    {
        return _authService.UserNameExistsAsync(userName, excludeId);
    }
}