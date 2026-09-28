using CompanyERP.Data;
using CompanyERP.Entities.Security;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Security;

public class SecuritySeederService : ISecuritySeederService
{
    private const string LegacySecurityPrefix = "SECURITY_";

    private readonly ApplicationDbContext _db;

    public SecuritySeederService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAndSaveAsync();
        await SeedPermissionsAndSaveAsync();
        await SeedRolePermissionsAsync();
        await SeedMenusAsync();
        await _db.SaveChangesAsync();
    }

    private async Task SeedRolesAndSaveAsync()
    {
        var roles = new[]
        {
            (SecurityDefs.SuperAdminRoleCode, "Super Admin", "Full system access.", true),
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
        // The catalog is the single source of truth shared with /Permission and the menu required
        // permissions, so seeding it here keeps the three from drifting apart.
        var added = false;
        foreach (var item in SecurityDefs.Catalog)
        {
            if (!await _db.Permissions.AnyAsync(p => p.Code == item.Code))
            {
                _db.Permissions.Add(new Permission
                {
                    Code = item.Code,
                    Name = item.Name,
                    Module = item.Module,
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

    private async Task SeedRolePermissionsAsync()
    {
        var permissions = await _db.Permissions.AsNoTracking().ToListAsync();
        var superAdmin = await _db.Roles.FirstOrDefaultAsync(r => r.Code == SecurityDefs.SuperAdminRoleCode);
        var admin = await _db.Roles.FirstOrDefaultAsync(r => r.Code == "ADMIN");

        if (superAdmin is not null)
        {
            await GrantAsync(superAdmin.Id, permissions.Select(p => p.Id));
        }

        if (admin is not null)
        {
            await GrantAsync(admin.Id, permissions.Where(p => p.Module != "Accounting").Select(p => p.Id));
        }
    }

    private async Task GrantAsync(int roleId, IEnumerable<int> permissionIds)
    {
        var granted = await _db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        var missing = permissionIds.Distinct().Where(id => !granted.Contains(id));
        foreach (var permissionId in missing)
        {
            _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
        }

        if (missing.Any())
        {
            await _db.SaveChangesAsync();
        }
    }

    private async Task SeedMenusAsync()
    {
        await NormaliseLegacySecurityCodesAsync();

        // Parents first so every child can be attached to an already-persisted parent row.
        var parents = new Dictionary<string, Menu>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in MenuCatalog.Items.Where(i => i.ParentCode is null))
        {
            parents[item.Code] = await EnsureMenuAsync(item, parent: null);
        }

        foreach (var item in MenuCatalog.Items.Where(i => i.ParentCode is not null))
        {
            if (parents.TryGetValue(item.ParentCode!, out var parent))
            {
                await EnsureMenuAsync(item, parent);
            }
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Earlier builds seeded the Security children as SECURITY_USERS, SECURITY_ROLES and so on. Renaming
    /// them onto the catalog codes lets a partially seeded database be repaired in place instead of
    /// ending up with two copies of every Security entry once the catalog is synced.
    /// </summary>
    private async Task NormaliseLegacySecurityCodesAsync()
    {
        var existing = await _db.Menus.ToListAsync();
        var changed = false;

        foreach (var legacy in existing.Where(m =>
                     m.Code.StartsWith(LegacySecurityPrefix, StringComparison.OrdinalIgnoreCase)))
        {
            var code = legacy.Code[LegacySecurityPrefix.Length..];

            if (existing.Any(m => m.Id != legacy.Id
                                  && string.Equals(m.Code, code, StringComparison.OrdinalIgnoreCase)))
            {
                // The catalog row is already present, so this one is a leftover duplicate. Retire it
                // instead of deleting, so an administrator can still find it in the menu list.
                legacy.IsActive = false;
                changed = true;
                continue;
            }

            legacy.Code = code;
            changed = true;
        }

        if (changed)
        {
            await _db.SaveChangesAsync();
        }
    }

    private async Task<Menu> EnsureMenuAsync(MenuCatalog.CatalogMenu item, Menu? parent)
    {
        var menu = await _db.Menus.FirstOrDefaultAsync(m => m.Code == item.Code);
        if (menu is not null)
        {
            // Repair a parent that was left null or points at the wrong row. Names, icons and required
            // permissions are left untouched so administrator customisations survive a restart.
            var parentId = parent?.Id;
            if (menu.ParentId != parentId)
            {
                menu.ParentId = parentId;
            }

            return menu;
        }

        menu = new Menu
        {
            Code = item.Code,
            Name = item.Name,
            Icon = item.Icon,
            Controller = item.Controller,
            Action = item.Action,
            PermissionCode = item.PermissionCode,
            ParentId = parent?.Id,
            DisplayOrder = item.DisplayOrder,
            IsSystem = true
        };

        _db.Menus.Add(menu);
        await _db.SaveChangesAsync();
        return menu;
    }
}
