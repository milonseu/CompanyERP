using CompanyERP.Data;
using CompanyERP.Entities.Expense;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Expense;

public class ExpenseTypeService : IExpenseTypeService
{
    private readonly ApplicationDbContext _db;

    public ExpenseTypeService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<ExpenseType>> GetAllAsync(int companyId)
    {
        return await _db.ExpenseTypes
            .AsNoTracking()
            .Where(t => t.CompanyId == companyId)
            .Include(t => t.ExpenseCategory)
            .OrderBy(t => t.Code)
            .ToListAsync();
    }

    public async Task<ExpenseType?> GetByIdAsync(int id)
    {
        return await _db.ExpenseTypes
            .AsNoTracking()
            .Include(t => t.ExpenseCategory)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<ExpenseType>> GetByCategoryAsync(int companyId, int categoryId)
    {
        return await _db.ExpenseTypes
            .AsNoTracking()
            .Where(t => t.CompanyId == companyId && t.ExpenseCategoryId == categoryId)
            .OrderBy(t => t.Code)
            .ToListAsync();
    }

    public async Task<(bool Success, string Error)> CreateAsync(ExpenseType type)
    {
        type.Code = string.IsNullOrWhiteSpace(type.Code) ? string.Empty : type.Code.Trim().ToUpperInvariant();
        type.Name = string.IsNullOrWhiteSpace(type.Name) ? string.Empty : type.Name.Trim();
        type.Description = string.IsNullOrWhiteSpace(type.Description) ? null : type.Description.Trim();

        if (string.IsNullOrWhiteSpace(type.Code) || string.IsNullOrWhiteSpace(type.Name))
        {
            return (false, "Type code and name are required.");
        }

        if (!await _db.ExpenseCategories.AnyAsync(c => c.Id == type.ExpenseCategoryId && c.CompanyId == type.CompanyId))
        {
            return (false, "Selected expense category does not belong to the company.");
        }

        var exists = await _db.ExpenseTypes.AnyAsync(t => t.CompanyId == type.CompanyId && t.Code == type.Code);
        if (exists)
        {
            return (false, $"Type code '{type.Code}' already exists for the company.");
        }

        _db.ExpenseTypes.Add(type);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(ExpenseType type)
    {
        var existing = await _db.ExpenseTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == type.Id);
        if (existing is null)
        {
            return (false, "Expense type not found.");
        }

        type.Code = string.IsNullOrWhiteSpace(type.Code) ? string.Empty : type.Code.Trim().ToUpperInvariant();
        type.Name = string.IsNullOrWhiteSpace(type.Name) ? string.Empty : type.Name.Trim();
        type.Description = string.IsNullOrWhiteSpace(type.Description) ? null : type.Description.Trim();

        if (string.IsNullOrWhiteSpace(type.Code) || string.IsNullOrWhiteSpace(type.Name))
        {
            return (false, "Type code and name are required.");
        }

        var duplicate = await _db.ExpenseTypes.AnyAsync(t => t.CompanyId == existing.CompanyId && t.Code == type.Code && t.Id != type.Id);
        if (duplicate)
        {
            return (false, $"Type code '{type.Code}' already exists for the company.");
        }

        type.CompanyId = existing.CompanyId;
        type.CreatedAt = existing.CreatedAt;
        type.CreatedBy = existing.CreatedBy;
        _db.ExpenseTypes.Update(type);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var type = await _db.ExpenseTypes.FindAsync(id);
        if (type is null)
        {
            return (false, "Expense type not found.");
        }

        if (await _db.ExpenseEntries.AnyAsync(e => e.ExpenseTypeId == id))
        {
            return (false, "Cannot delete a type that has expense entries.");
        }

        _db.ExpenseTypes.Remove(type);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}