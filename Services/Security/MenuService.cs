using CompanyERP.Data;
using CompanyERP.Entities.Security;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Security;

public class MenuService : IMenuService
{
    private readonly ApplicationDbContext _db;

    public MenuService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Menu>> GetAllAsync()
    {
        return await _db.Menus
            .Include(m => m.Children)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();
    }

    public async Task<Menu?> GetByIdAsync(int id)
    {
        return await _db.Menus.FindAsync(id);
    }

    public async Task<(bool Success, string Error)> CreateAsync(Menu menu)
    {
        menu.Code = string.IsNullOrWhiteSpace(menu.Code) ? string.Empty : menu.Code.Trim().ToUpperInvariant();
        menu.Name = string.IsNullOrWhiteSpace(menu.Name) ? string.Empty : menu.Name.Trim();

        if (string.IsNullOrWhiteSpace(menu.Code) || string.IsNullOrWhiteSpace(menu.Name))
        {
            return (false, "Menu code and name are required.");
        }

        if (menu.ParentId.HasValue && !await _db.Menus.AnyAsync(m => m.Id == menu.ParentId.Value))
        {
            return (false, "Selected parent menu does not exist.");
        }

        if (await CodeExistsAsync(menu.Code))
        {
            return (false, "Menu code already exists.");
        }

        _db.Menus.Add(menu);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(Menu menu)
    {
        var existing = await _db.Menus.AsNoTracking().FirstOrDefaultAsync(m => m.Id == menu.Id);
        if (existing is null)
        {
            return (false, "Menu not found.");
        }

        if (menu.ParentId == menu.Id)
        {
            return (false, "A menu cannot be its own parent.");
        }

        if (menu.ParentId.HasValue && !await _db.Menus.AnyAsync(m => m.Id == menu.ParentId.Value))
        {
            return (false, "Selected parent menu does not exist.");
        }

        menu.IsSystem = existing.IsSystem;
        menu.CreatedAt = existing.CreatedAt;
        menu.CreatedBy = existing.CreatedBy;

        _db.Menus.Update(menu);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var menu = await _db.Menus.FindAsync(id);
        if (menu is null)
        {
            return (false, "Menu not found.");
        }

        if (menu.IsSystem)
        {
            return (false, "System menus cannot be deleted.");
        }

        if (await HasChildrenAsync(id))
        {
            return (false, "Menu cannot be deleted because it has sub menus.");
        }

        _db.Menus.Remove(menu);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
    {
        var query = _db.Menus.AsNoTracking().Where(m => m.Code == code);
        if (excludeId.HasValue)
        {
            query = query.Where(m => m.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<List<Menu>> GetTopLevelAsync()
    {
        return await _db.Menus
            .Where(m => m.ParentId == null)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();
    }

    public async Task<List<Menu>> GetSubMenusAsync(int parentId)
    {
        return await _db.Menus
            .Where(m => m.ParentId == parentId)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();
    }

    public async Task<bool> HasChildrenAsync(int id)
    {
        return await _db.Menus.AnyAsync(m => m.ParentId == id);
    }
}