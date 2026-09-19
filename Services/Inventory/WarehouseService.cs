using CompanyERP.Data;
using CompanyERP.Entities.Inventory;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Inventory;

public class WarehouseService : IWarehouseService
{
    private readonly ApplicationDbContext _db;

    public WarehouseService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Warehouse>> GetAllAsync()
    {
        return await _db.Warehouses
            .Include(w => w.Company)
            .Include(w => w.Branch)
            .OrderBy(w => w.Company != null ? w.Company.Name : string.Empty)
            .ThenBy(w => w.Name)
            .ToListAsync();
    }

    public async Task<List<Warehouse>> GetByCompanyIdAsync(int companyId)
    {
        return await _db.Warehouses
            .Where(w => w.CompanyId == companyId)
            .OrderBy(w => w.Name)
            .ToListAsync();
    }

    public async Task<Warehouse?> GetByIdAsync(int id)
    {
        return await _db.Warehouses
            .Include(w => w.Company)
            .Include(w => w.Branch)
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId = null)
    {
        var query = _db.Warehouses.Where(w => w.Code == code && w.CompanyId == companyId);
        if (excludeId.HasValue)
        {
            query = query.Where(w => w.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<bool> HasStockAsync(int id)
    {
        return await _db.StockBalances.AnyAsync(sb => sb.WarehouseId == id && sb.Quantity != 0);
    }

    public async Task<(bool Success, string Error)> CreateAsync(Warehouse warehouse)
    {
        warehouse.Code = string.IsNullOrWhiteSpace(warehouse.Code) ? string.Empty : warehouse.Code.Trim().ToUpperInvariant();
        warehouse.Name = string.IsNullOrWhiteSpace(warehouse.Name) ? string.Empty : warehouse.Name.Trim();

        if (string.IsNullOrWhiteSpace(warehouse.Code) || string.IsNullOrWhiteSpace(warehouse.Name))
        {
            return (false, "Warehouse code and name are required.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == warehouse.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (!await _db.Branches.AnyAsync(b => b.Id == warehouse.BranchId))
        {
            return (false, "Selected branch does not exist.");
        }

        if (await CodeExistsAsync(warehouse.Code, warehouse.CompanyId))
        {
            return (false, $"Warehouse code already exists for company {warehouse.CompanyId}.");
        }

        _db.Warehouses.Add(warehouse);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(Warehouse warehouse)
    {
        warehouse.Code = string.IsNullOrWhiteSpace(warehouse.Code) ? string.Empty : warehouse.Code.Trim().ToUpperInvariant();
        warehouse.Name = string.IsNullOrWhiteSpace(warehouse.Name) ? string.Empty : warehouse.Name.Trim();

        var existing = await _db.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == warehouse.Id);
        if (existing is null)
        {
            return (false, "Warehouse not found.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == warehouse.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (!await _db.Branches.AnyAsync(b => b.Id == warehouse.BranchId))
        {
            return (false, "Selected branch does not exist.");
        }

        if (await CodeExistsAsync(warehouse.Code, warehouse.CompanyId, warehouse.Id))
        {
            return (false, $"Warehouse code already exists for company {warehouse.CompanyId}.");
        }

        warehouse.CreatedAt = existing.CreatedAt;
        warehouse.CreatedBy = existing.CreatedBy;

        _db.Warehouses.Update(warehouse);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var warehouse = await _db.Warehouses.FindAsync(id);
        if (warehouse is null)
        {
            return (false, "Warehouse not found.");
        }

        if (await HasStockAsync(id))
        {
            return (false, "Warehouse cannot be deleted because it has stock.");
        }

        _db.Warehouses.Remove(warehouse);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}