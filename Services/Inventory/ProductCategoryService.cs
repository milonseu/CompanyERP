using CompanyERP.Data;
using CompanyERP.Entities.Inventory;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Inventory;

public class ProductCategoryService : IProductCategoryService
{
    private readonly ApplicationDbContext _db;

    public ProductCategoryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<ProductCategory>> GetAllAsync()
    {
        return await _db.ProductCategories
            .Include(c => c.Company)
            .OrderBy(c => c.Company != null ? c.Company.Name : string.Empty)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<ProductCategory>> GetByCompanyIdAsync(int companyId)
    {
        return await _db.ProductCategories
            .Where(c => c.CompanyId == companyId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<ProductCategory?> GetByIdAsync(int id)
    {
        return await _db.ProductCategories
            .Include(c => c.Company)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<bool> NameExistsAsync(string name, int companyId, int? excludeId = null)
    {
        var query = _db.ProductCategories.Where(c => c.Name == name && c.CompanyId == companyId);
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<bool> HasProductsAsync(int id)
    {
        return await _db.Products.AnyAsync(p => p.CategoryId == id);
    }

    public async Task<(bool Success, string Error)> CreateAsync(ProductCategory category)
    {
        category.Name = string.IsNullOrWhiteSpace(category.Name) ? string.Empty : category.Name.Trim();

        if (string.IsNullOrWhiteSpace(category.Name))
        {
            return (false, "Category name is required.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == category.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (await NameExistsAsync(category.Name, category.CompanyId))
        {
            return (false, $"Category already exists for company {category.CompanyId}.");
        }

        _db.ProductCategories.Add(category);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(ProductCategory category)
    {
        category.Name = string.IsNullOrWhiteSpace(category.Name) ? string.Empty : category.Name.Trim();

        var existing = await _db.ProductCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == category.Id);
        if (existing is null)
        {
            return (false, "Category not found.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == category.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (await NameExistsAsync(category.Name, category.CompanyId, category.Id))
        {
            return (false, $"Category already exists for company {category.CompanyId}.");
        }

        category.CreatedAt = existing.CreatedAt;
        category.CreatedBy = existing.CreatedBy;

        _db.ProductCategories.Update(category);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var category = await _db.ProductCategories.FindAsync(id);
        if (category is null)
        {
            return (false, "Category not found.");
        }

        if (await HasProductsAsync(id))
        {
            return (false, "Category cannot be deleted because it has products.");
        }

        _db.ProductCategories.Remove(category);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}