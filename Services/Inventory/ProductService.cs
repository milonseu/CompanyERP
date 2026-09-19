using CompanyERP.Data;
using CompanyERP.Entities.Inventory;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Inventory;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _db;

    public ProductService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Product>> GetAllAsync()
    {
        return await _db.Products
            .Include(p => p.Company)
            .Include(p => p.Category)
            .OrderBy(p => p.Company != null ? p.Company.Name : string.Empty)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<List<Product>> GetByCompanyIdAsync(int companyId)
    {
        return await _db.Products
            .Where(p => p.CompanyId == companyId)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _db.Products
            .Include(p => p.Company)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId = null)
    {
        var query = _db.Products.Where(p => p.Code == code && p.CompanyId == companyId);
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<bool> HasStockAsync(int id)
    {
        return await _db.StockBalances.AnyAsync(sb => sb.ProductId == id && sb.Quantity != 0);
    }

    public async Task<(bool Success, string Error)> CreateAsync(Product product)
    {
        product.Code = string.IsNullOrWhiteSpace(product.Code) ? string.Empty : product.Code.Trim().ToUpperInvariant();
        product.Name = string.IsNullOrWhiteSpace(product.Name) ? string.Empty : product.Name.Trim();

        if (string.IsNullOrWhiteSpace(product.Code) || string.IsNullOrWhiteSpace(product.Name))
        {
            return (false, "Product code and name are required.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == product.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (!await _db.ProductCategories.AnyAsync(c => c.Id == product.CategoryId && c.CompanyId == product.CompanyId))
        {
            return (false, "Selected category does not belong to the selected company.");
        }

        if (await CodeExistsAsync(product.Code, product.CompanyId))
        {
            return (false, $"Product code already exists for company {product.CompanyId}.");
        }

        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(Product product)
    {
        product.Code = string.IsNullOrWhiteSpace(product.Code) ? string.Empty : product.Code.Trim().ToUpperInvariant();
        product.Name = string.IsNullOrWhiteSpace(product.Name) ? string.Empty : product.Name.Trim();

        var existing = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == product.Id);
        if (existing is null)
        {
            return (false, "Product not found.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == product.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (!await _db.ProductCategories.AnyAsync(c => c.Id == product.CategoryId && c.CompanyId == product.CompanyId))
        {
            return (false, "Selected category does not belong to the selected company.");
        }

        if (await CodeExistsAsync(product.Code, product.CompanyId, product.Id))
        {
            return (false, $"Product code already exists for company {product.CompanyId}.");
        }

        product.CreatedAt = existing.CreatedAt;
        product.CreatedBy = existing.CreatedBy;

        _db.Products.Update(product);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null)
        {
            return (false, "Product not found.");
        }

        if (await HasStockAsync(id))
        {
            return (false, "Product cannot be deleted because it has stock.");
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}