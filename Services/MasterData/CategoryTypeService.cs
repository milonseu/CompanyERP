using CompanyERP.Data;
using CompanyERP.Entities.MasterData;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.MasterData;

public class CategoryTypeService : ICategoryTypeService
{
    private readonly ApplicationDbContext _db;

    public CategoryTypeService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<CategoryType>> GetAllAsync()
    {
        return await _db.CategoryTypes
            .Include(t => t.Company)
            .OrderBy(t => t.Company != null ? t.Company.Name : string.Empty)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<List<CategoryType>> GetByCompanyIdAsync(int companyId)
    {
        return await _db.CategoryTypes
            .Where(t => t.CompanyId == companyId)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<CategoryType?> GetByIdAsync(int id)
    {
        return await _db.CategoryTypes
            .Include(t => t.Company)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId = null)
    {
        var query = _db.CategoryTypes.Where(t => t.Code == code && t.CompanyId == companyId);
        if (excludeId.HasValue)
        {
            query = query.Where(t => t.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<bool> HasCategoriesAsync(int id)
    {
        return await _db.Categories.AnyAsync(c => c.CategoryTypeId == id);
    }

    public async Task<(bool Success, string Error)> CreateAsync(CategoryType categoryType)
    {
        categoryType.Code = string.IsNullOrWhiteSpace(categoryType.Code) ? string.Empty : categoryType.Code.Trim();
        categoryType.Name = string.IsNullOrWhiteSpace(categoryType.Name) ? string.Empty : categoryType.Name.Trim();

        if (string.IsNullOrWhiteSpace(categoryType.Code))
        {
            return (false, "Category type code is required.");
        }

        if (string.IsNullOrWhiteSpace(categoryType.Name))
        {
            return (false, "Category type name is required.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == categoryType.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (await CodeExistsAsync(categoryType.Code, categoryType.CompanyId))
        {
            return (false, $"Category type code {categoryType.Code} already exists for the selected company.");
        }

        _db.CategoryTypes.Add(categoryType);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(CategoryType categoryType)
    {
        categoryType.Code = string.IsNullOrWhiteSpace(categoryType.Code) ? string.Empty : categoryType.Code.Trim();
        categoryType.Name = string.IsNullOrWhiteSpace(categoryType.Name) ? string.Empty : categoryType.Name.Trim();

        var existing = await _db.CategoryTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == categoryType.Id);
        if (existing is null)
        {
            return (false, "Category type not found.");
        }

        if (string.IsNullOrWhiteSpace(categoryType.Code))
        {
            return (false, "Category type code is required.");
        }

        if (string.IsNullOrWhiteSpace(categoryType.Name))
        {
            return (false, "Category type name is required.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == categoryType.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (await CodeExistsAsync(categoryType.Code, categoryType.CompanyId, categoryType.Id))
        {
            return (false, $"Category type code {categoryType.Code} already exists for the selected company.");
        }

        categoryType.CreatedAt = existing.CreatedAt;
        categoryType.CreatedBy = existing.CreatedBy;

        _db.CategoryTypes.Update(categoryType);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var categoryType = await _db.CategoryTypes.FindAsync(id);
        if (categoryType is null)
        {
            return (false, "Category type not found.");
        }

        if (await HasCategoriesAsync(id))
        {
            return (false, "Category type cannot be deleted because categories exist under it.");
        }

        _db.CategoryTypes.Remove(categoryType);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}