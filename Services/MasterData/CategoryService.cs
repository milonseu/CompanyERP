using CompanyERP.Data;
using CompanyERP.Entities.MasterData;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.MasterData;

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _db;

    public CategoryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        return await _db.Categories
            .Include(c => c.Company)
            .Include(c => c.CategoryType)
            .OrderBy(c => c.Company != null ? c.Company.Name : string.Empty)
            .ThenBy(c => c.CategoryType != null ? c.CategoryType.Name : string.Empty)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<Category>> GetByCompanyIdAsync(int companyId)
    {
        return await _db.Categories
            .Where(c => c.CompanyId == companyId)
            .Include(c => c.CategoryType)
            .OrderBy(c => c.CategoryType != null ? c.CategoryType.Name : string.Empty)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<Category>> GetByCompanyAndTypeAsync(int companyId, int categoryTypeId)
    {
        return await _db.Categories
            .Where(c => c.CompanyId == companyId && c.CategoryTypeId == categoryTypeId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        return await _db.Categories
            .Include(c => c.Company)
            .Include(c => c.CategoryType)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId = null)
    {
        var query = _db.Categories.Where(c => c.Code == code && c.CompanyId == companyId);
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<(bool Success, string Error)> CreateAsync(Category category)
    {
        category.Code = string.IsNullOrWhiteSpace(category.Code) ? string.Empty : category.Code.Trim();
        category.Name = string.IsNullOrWhiteSpace(category.Name) ? string.Empty : category.Name.Trim();

        if (string.IsNullOrWhiteSpace(category.Code))
        {
            return (false, "Category code is required.");
        }

        if (string.IsNullOrWhiteSpace(category.Name))
        {
            return (false, "Category name is required.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == category.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (!await _db.CategoryTypes.AnyAsync(t => t.Id == category.CategoryTypeId && t.CompanyId == category.CompanyId))
        {
            return (false, "Selected category type does not belong to the company.");
        }

        if (await CodeExistsAsync(category.Code, category.CompanyId))
        {
            return (false, $"Category code {category.Code} already exists for the selected company.");
        }

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(Category category)
    {
        category.Code = string.IsNullOrWhiteSpace(category.Code) ? string.Empty : category.Code.Trim();
        category.Name = string.IsNullOrWhiteSpace(category.Name) ? string.Empty : category.Name.Trim();

        var existing = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == category.Id);
        if (existing is null)
        {
            return (false, "Category not found.");
        }

        if (string.IsNullOrWhiteSpace(category.Code))
        {
            return (false, "Category code is required.");
        }

        if (string.IsNullOrWhiteSpace(category.Name))
        {
            return (false, "Category name is required.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == category.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (!await _db.CategoryTypes.AnyAsync(t => t.Id == category.CategoryTypeId && t.CompanyId == category.CompanyId))
        {
            return (false, "Selected category type does not belong to the company.");
        }

        if (await CodeExistsAsync(category.Code, category.CompanyId, category.Id))
        {
            return (false, $"Category code {category.Code} already exists for the selected company.");
        }

        category.CreatedAt = existing.CreatedAt;
        category.CreatedBy = existing.CreatedBy;

        _db.Categories.Update(category);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null)
        {
            return (false, "Category not found.");
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}