using CompanyERP.Data;
using CompanyERP.Entities.Asset;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Asset;

public class AssetTypeService : IAssetTypeService
{
    private readonly ApplicationDbContext _db;

    public AssetTypeService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<AssetType>> GetAllAsync(int companyId)
    {
        return await _db.AssetTypes
            .AsNoTracking()
            .Where(t => t.CompanyId == companyId)
            .Include(t => t.AssetCategory)
            .OrderBy(t => t.Code)
            .ToListAsync();
    }

    public async Task<AssetType?> GetByIdAsync(int id)
    {
        return await _db.AssetTypes
            .AsNoTracking()
            .Include(t => t.AssetCategory)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<AssetType>> GetByCategoryAsync(int companyId, int categoryId)
    {
        return await _db.AssetTypes
            .AsNoTracking()
            .Where(t => t.CompanyId == companyId && t.AssetCategoryId == categoryId)
            .OrderBy(t => t.Code)
            .ToListAsync();
    }

    public async Task<(bool Success, string Error)> CreateAsync(AssetType type)
    {
        type.Code = string.IsNullOrWhiteSpace(type.Code) ? string.Empty : type.Code.Trim().ToUpperInvariant();
        type.Name = string.IsNullOrWhiteSpace(type.Name) ? string.Empty : type.Name.Trim();
        type.Description = string.IsNullOrWhiteSpace(type.Description) ? null : type.Description.Trim();

        if (string.IsNullOrWhiteSpace(type.Code) || string.IsNullOrWhiteSpace(type.Name))
        {
            return (false, "Type code and name are required.");
        }

        if (!await _db.AssetCategories.AnyAsync(c => c.Id == type.AssetCategoryId && c.CompanyId == type.CompanyId))
        {
            return (false, "Selected asset category does not belong to the company.");
        }

        var exists = await _db.AssetTypes.AnyAsync(t => t.CompanyId == type.CompanyId && t.Code == type.Code);
        if (exists)
        {
            return (false, $"Type code '{type.Code}' already exists for the company.");
        }

        _db.AssetTypes.Add(type);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(AssetType type)
    {
        var existing = await _db.AssetTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == type.Id);
        if (existing is null)
        {
            return (false, "Asset type not found.");
        }

        type.Code = string.IsNullOrWhiteSpace(type.Code) ? string.Empty : type.Code.Trim().ToUpperInvariant();
        type.Name = string.IsNullOrWhiteSpace(type.Name) ? string.Empty : type.Name.Trim();
        type.Description = string.IsNullOrWhiteSpace(type.Description) ? null : type.Description.Trim();

        if (string.IsNullOrWhiteSpace(type.Code) || string.IsNullOrWhiteSpace(type.Name))
        {
            return (false, "Type code and name are required.");
        }

        var duplicate = await _db.AssetTypes.AnyAsync(t => t.CompanyId == existing.CompanyId && t.Code == type.Code && t.Id != type.Id);
        if (duplicate)
        {
            return (false, $"Type code '{type.Code}' already exists for the company.");
        }

        type.CompanyId = existing.CompanyId;
        type.CreatedAt = existing.CreatedAt;
        type.CreatedBy = existing.CreatedBy;
        _db.AssetTypes.Update(type);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var type = await _db.AssetTypes.FindAsync(id);
        if (type is null)
        {
            return (false, "Asset type not found.");
        }

        if (await _db.AssetRegisters.AnyAsync(a => a.AssetTypeId == id))
        {
            return (false, "Cannot delete a type that has registered assets.");
        }

        _db.AssetTypes.Remove(type);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}