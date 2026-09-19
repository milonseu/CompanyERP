using CompanyERP.Data;
using CompanyERP.Entities.Asset;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Asset;

public class AssetCategoryService : IAssetCategoryService
{
    private readonly ApplicationDbContext _db;

    public AssetCategoryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<AssetCategory>> GetAllAsync(int companyId)
    {
        return await _db.AssetCategories
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .OrderBy(c => c.Code)
            .ToListAsync();
    }

    public async Task<AssetCategory?> GetByIdAsync(int id)
    {
        return await _db.AssetCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<(bool Success, string Error)> CreateAsync(AssetCategory category)
    {
        category.Code = string.IsNullOrWhiteSpace(category.Code) ? string.Empty : category.Code.Trim().ToUpperInvariant();
        category.Name = string.IsNullOrWhiteSpace(category.Name) ? string.Empty : category.Name.Trim();
        category.Description = string.IsNullOrWhiteSpace(category.Description) ? null : category.Description.Trim();

        if (string.IsNullOrWhiteSpace(category.Code) || string.IsNullOrWhiteSpace(category.Name))
        {
            return (false, "Category code and name are required.");
        }

        var exists = await _db.AssetCategories.AnyAsync(c => c.CompanyId == category.CompanyId && c.Code == category.Code);
        if (exists)
        {
            return (false, $"Category code '{category.Code}' already exists for the company.");
        }

        _db.AssetCategories.Add(category);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(AssetCategory category)
    {
        var existing = await _db.AssetCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == category.Id);
        if (existing is null)
        {
            return (false, "Asset category not found.");
        }

        category.Code = string.IsNullOrWhiteSpace(category.Code) ? string.Empty : category.Code.Trim().ToUpperInvariant();
        category.Name = string.IsNullOrWhiteSpace(category.Name) ? string.Empty : category.Name.Trim();
        category.Description = string.IsNullOrWhiteSpace(category.Description) ? null : category.Description.Trim();

        if (string.IsNullOrWhiteSpace(category.Code) || string.IsNullOrWhiteSpace(category.Name))
        {
            return (false, "Category code and name are required.");
        }

        var duplicate = await _db.AssetCategories.AnyAsync(c => c.CompanyId == existing.CompanyId && c.Code == category.Code && c.Id != category.Id);
        if (duplicate)
        {
            return (false, $"Category code '{category.Code}' already exists for the company.");
        }

        category.CompanyId = existing.CompanyId;
        category.CreatedAt = existing.CreatedAt;
        category.CreatedBy = existing.CreatedBy;
        _db.AssetCategories.Update(category);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var category = await _db.AssetCategories.FindAsync(id);
        if (category is null)
        {
            return (false, "Asset category not found.");
        }

        if (await _db.AssetTypes.AnyAsync(t => t.AssetCategoryId == id))
        {
            return (false, "Cannot delete a category that has asset types.");
        }

        _db.AssetCategories.Remove(category);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}