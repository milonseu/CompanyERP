using CompanyERP.Data;
using CompanyERP.Entities.Security;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Security;

public class SecuritySeederService : ISecuritySeederService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuthService _authService;

    public SecuritySeederService(ApplicationDbContext db, IAuthService authService)
    {
        _db = db;
        _authService = authService;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAndSaveAsync();
        await SeedPermissionsAndSaveAsync();
        await SeedRolePermissionsAsync();
        await SeedMenusAndSaveAsync();
        await SeedDefaultAdminAsync();
        await _db.SaveChangesAsync();
    }

    private async Task SeedRolesAndSaveAsync()
    {
        var roles = new[]
        {
            ("SUPERADMIN", "Super Admin", "Full system access.", true),
            ("ADMIN", "Admin", "Administrative access.", true),
            ("ACCOUNTS", "Accounts", "Accounting and finance operations.", false),
            ("SALES", "Sales", "Sales operations.", false),
            ("INVENTORY_OPERATOR", "Inventory Operator", "Inventory management.", false),
            ("ASSET_OPERATOR", "Asset Operator", "Asset management.", false)
        };

        var added = false;
        foreach (var (code, name, description, isSystem) in roles)
        {
            if (!await _db.Roles.AnyAsync(r => r.Code == code))
            {
                _db.Roles.Add(new Role { Code = code, Name = name, Description = description, IsSystem = isSystem });
                added = true;
            }
        }

        if (added)
        {
            await _db.SaveChangesAsync();
        }
    }

    private async Task SeedPermissionsAndSaveAsync()
    {
        var modules = new[]
        {
            "Company", "MasterData", "Branch", "Customer", "Supplier", "Employee",
            "Expense", "Inventory", "Asset", "Purchase", "Sales", "Payment", "Security"
        };

        var permissions = new List<(string Code, string Name, string Module)>();

        foreach (var module in modules)
        {
            foreach (var action in new[] { "View", "Create", "Edit", "Delete" })
            {
                permissions.Add(($"{module}.{action}", $"{module} - {action}", module));
            }
        }

        permissions.Add(("Accounting.View", "Accounting - View", "Accounting"));
        permissions.Add(("Accounting.Create", "Accounting - Create", "Accounting"));
        permissions.Add(("Accounting.Post", "Accounting - Post", "Accounting"));
        permissions.Add(("Accounting.Edit", "Accounting - Edit", "Accounting"));
        permissions.Add(("Accounting.Delete", "Accounting - Delete", "Accounting"));
        permissions.Add(("Accounting.Approve", "Accounting - Approve", "Accounting"));
        permissions.Add(("Accounting.Close", "Accounting - Close", "Accounting"));

        var added = false;
        foreach (var (code, name, module) in permissions)
        {
            if (!await _db.Permissions.AnyAsync(p => p.Code == code))
            {
                _db.Permissions.Add(new Permission { Code = code, Name = name, Module = module, IsSystem = true });
                added = true;
            }
        }

        if (added)
        {
            await _db.SaveChangesAsync();
        }
    }

    private async Task SeedRolePermissionsAsync()
    {
        var allCodes = await _db.Permissions.Select(p => p.Code).ToListAsync();
        var superAdmin = await _db.Roles.FirstOrDefaultAsync(r => r.Code == "SUPERADMIN");
        var admin = await _db.Roles.FirstOrDefaultAsync(r => r.Code == "ADMIN");

        if (superAdmin is not null)
        {
            foreach (var code in allCodes)
            {
                var permission = await _db.Permissions.FirstOrDefaultAsync(p => p.Code == code);
                if (permission is not null &&
                    !await _db.RolePermissions.AnyAsync(rp => rp.RoleId == superAdmin.Id && rp.PermissionId == permission.Id))
                {
                    _db.RolePermissions.Add(new RolePermission { RoleId = superAdmin.Id, PermissionId = permission.Id });
                }
            }
        }

        if (admin is not null)
        {
            foreach (var code in allCodes.Where(c => !c.StartsWith("Accounting.")))
            {
                var permission = await _db.Permissions.FirstOrDefaultAsync(p => p.Code == code);
                if (permission is not null &&
                    !await _db.RolePermissions.AnyAsync(rp => rp.RoleId == admin.Id && rp.PermissionId == permission.Id))
                {
                    _db.RolePermissions.Add(new RolePermission { RoleId = admin.Id, PermissionId = permission.Id });
                }
            }
        }
    }

    private async Task SeedMenusAndSaveAsync()
    {
        var parent = await _db.Menus.FirstOrDefaultAsync(m => m.Code == "SECURITY");
        if (parent is null)
        {
            parent = new Menu
            {
                Code = "SECURITY",
                Name = "Security & Permission",
                Icon = "bi-shield-lock",
                DisplayOrder = 99,
                IsSystem = true
            };
            _db.Menus.Add(parent);
            await _db.SaveChangesAsync();
        }

        // Fix children that were previously created with a null parent.
        var existingChildren = await _db.Menus
            .Where(m => m.Code.StartsWith("SECURITY_") && m.ParentId != parent.Id)
            .ToListAsync();

        foreach (var child in existingChildren)
        {
            child.ParentId = parent.Id;
        }

        if (existingChildren.Count > 0)
        {
            await _db.SaveChangesAsync();
        }

        var children = new[]
        {
            ("SECURITY_USERS", "Users", "bi-person", "User", "Index", 1),
            ("SECURITY_ROLES", "Roles", "bi-person-badge", "Role", "Index", 2),
            ("SECURITY_PERMISSIONS", "Permissions", "bi-key", "Permission", "Index", 3),
            ("SECURITY_MENUS", "Menus", "bi-menu-button-wide", "Menu", "Index", 4),
            ("SECURITY_ACTIVITY_LOG", "Activity Log", "bi-clock-history", "ActivityLog", "Index", 5),
            ("SECURITY_LOGIN_HISTORY", "Login History", "bi-box-arrow-in-right", "LoginHistory", "Index", 6)
        };

        var added = false;
        foreach (var (code, name, icon, controller, action, order) in children)
        {
            if (!await _db.Menus.AnyAsync(m => m.Code == code))
            {
                _db.Menus.Add(new Menu
                {
                    Code = code,
                    Name = name,
                    Icon = icon,
                    Controller = controller,
                    Action = action,
                    ParentId = parent.Id,
                    DisplayOrder = order,
                    IsSystem = true
                });
                added = true;
            }
        }

        if (added)
        {
            await _db.SaveChangesAsync();
        }
    }

    private async Task SeedDefaultAdminAsync()
    {
        const string adminUserName = "admin";
        const string adminPassword = "Admin@123";
        const string adminRoleCode = "SUPERADMIN";

        var admin = await _db.Users.FirstOrDefaultAsync(u => u.UserName == adminUserName);
        if (admin is null)
        {
            admin = new User
            {
                UserName = adminUserName,
                FullName = "System Administrator",
                Email = "admin@company.local",
                IsSystem = true,
                IsActive = true,
                PasswordHash = _authService.HashPassword(adminPassword)
            };
            _db.Users.Add(admin);
            await _db.SaveChangesAsync();
        }

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Code == adminRoleCode);
        if (role is not null && !await _db.UserRoles.AnyAsync(ur => ur.UserId == admin.Id && ur.RoleId == role.Id))
        {
            _db.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = role.Id });
        }
    }
}