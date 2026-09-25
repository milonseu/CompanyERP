using System.Security.Cryptography;
using CompanyERP.Data;
using CompanyERP.Entities.Security;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Security;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;

    public AuthService(ApplicationDbContext db)
    {
        _db = db;
    }

    private const string SuperAdminRole = "SUPERADMIN";

    public async Task<(bool Success, string Error, User? User)> ValidateAsync(string userName, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == userName);
        if (user is null)
        {
            return (false, "Invalid user name or password.", null);
        }

        if (!user.IsActive)
        {
            return (false, "This account is inactive.", null);
        }

        if (!VerifyPassword(password, user.PasswordHash))
        {
            return (false, "Invalid user name or password.", null);
        }

        return (true, string.Empty, user);
    }

    public async Task<User?> GetByUserNameAsync(string userName)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.UserName == userName);
    }

    public async Task<List<string>> GetPermissionCodesAsync(int userId)
    {
        var rolePermissionCodes = await (from rp in _db.RolePermissions
                                         join r in _db.Roles on rp.RoleId equals r.Id
                                         join ur in _db.UserRoles on r.Id equals ur.RoleId
                                         where ur.UserId == userId
                                         select rp.Permission!.Code).ToListAsync();

        var userPermissionCodes = await (from up in _db.UserPermissions
                                         join p in _db.Permissions on up.PermissionId equals p.Id
                                         where up.UserId == userId
                                         select p.Code).ToListAsync();

        return rolePermissionCodes.Concat(userPermissionCodes).Distinct().ToList();
    }

    public async Task<List<string>> GetRoleCodesAsync(int userId)
    {
        return await (from ur in _db.UserRoles
                      join r in _db.Roles on ur.RoleId equals r.Id
                      where ur.UserId == userId
                      select r.Code).ToListAsync();
    }

    public async Task<bool> HasPermissionAsync(int userId, string permissionCode)
    {
        if (await IsSuperAdminAsync(userId))
        {
            return true;
        }

        var codes = await GetPermissionCodesAsync(userId);
        return codes.Contains(permissionCode);
    }

    public async Task<bool> IsSuperAdminAsync(int userId)
    {
        var roles = await GetRoleCodesAsync(userId);
        return roles.Contains(SuperAdminRole);
    }

    public async Task<bool> UserNameExistsAsync(string userName, int? excludeId = null)
    {
        var query = _db.Users.AsNoTracking().Where(u => u.UserName == userName);
        if (excludeId.HasValue)
        {
            query = query.Where(u => u.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public string HashPassword(string password)
    {
        // PBKDF2 (RFC 2898) with SHA256, 100,000 iterations, 16-byte salt, 32-byte key.
        const int iterations = 100_000;
        const int saltSize = 16;
        const int keySize = 32;

        var salt = RandomNumberGenerator.GetBytes(saltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, keySize);

        return $"{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            var parts = hash.Split('.');
            if (parts.Length != 3)
            {
                return false;
            }

            var iterations = int.Parse(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);
            var expected = Convert.FromBase64String(parts[2]);

            var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

            return CryptographicOperations.FixedTimeEquals(key, expected);
        }
        catch
        {
            return false;
        }
    }

    public async Task RecordLoginSuccessAsync(int userId, string? ip)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
        {
            return;
        }

        user.LastLoginAt = DateTime.Now;

        _db.LoginHistories.Add(new LoginHistory
        {
            UserName = user.UserName,
            LoginTime = DateTime.Now,
            IsSuccess = true,
            IpAddress = ip
        });

        await _db.SaveChangesAsync();
    }

    public async Task RecordLoginFailAsync(string userName, string? ip, string failReason)
    {
        _db.LoginHistories.Add(new LoginHistory
        {
            UserName = userName,
            LoginTime = DateTime.Now,
            IsSuccess = false,
            IpAddress = ip,
            FailReason = failReason
        });

        await _db.SaveChangesAsync();
    }

    public async Task RecordLogoutAsync(int userId)
    {
        var userName = await _db.Users.Where(u => u.Id == userId).Select(u => u.UserName).FirstOrDefaultAsync();
        if (string.IsNullOrWhiteSpace(userName))
        {
            return;
        }

        var last = _db.LoginHistories
            .Where(h => h.UserName == userName && h.LogoutTime == null)
            .OrderByDescending(h => h.LoginTime)
            .FirstOrDefault();

        if (last is not null)
        {
            last.LogoutTime = DateTime.Now;
            await _db.SaveChangesAsync();
        }
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
}