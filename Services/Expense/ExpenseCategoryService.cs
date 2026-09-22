using CompanyERP.Data;
using CompanyERP.Entities.Expense;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Expense;

public class ExpenseCategoryService : IExpenseCategoryService
{
    private readonly ApplicationDbContext _db;

    public ExpenseCategoryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<ExpenseCategory>> GetAllAsync(int companyId)
    {
        return await _db.ExpenseCategories
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .OrderBy(c => c.Code)
            .ToListAsync();
    }

    public async Task<ExpenseCategory?> GetByIdAsync(int id)
    {
        return await _db.ExpenseCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<(bool Success, string Error)> CreateAsync(ExpenseCategory category)
    {
        category.Code = string.IsNullOrWhiteSpace(category.Code) ? string.Empty : category.Code.Trim().ToUpperInvariant();
        category.Name = string.IsNullOrWhiteSpace(category.Name) ? string.Empty : category.Name.Trim();
        category.Description = string.IsNullOrWhiteSpace(category.Description) ? null : category.Description.Trim();

        if (string.IsNullOrWhiteSpace(category.Code) || string.IsNullOrWhiteSpace(category.Name))
        {
            return (false, "Category code and name are required.");
        }

        var exists = await _db.ExpenseCategories.AnyAsync(c => c.CompanyId == category.CompanyId && c.Code == category.Code);
        if (exists)
        {
            return (false, $"Category code '{category.Code}' already exists for the company.");
        }

        _db.ExpenseCategories.Add(category);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(ExpenseCategory category)
    {
        var existing = await _db.ExpenseCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == category.Id);
        if (existing is null)
        {
            return (false, "Expense category not found.");
        }

        category.Code = string.IsNullOrWhiteSpace(category.Code) ? string.Empty : category.Code.Trim().ToUpperInvariant();
        category.Name = string.IsNullOrWhiteSpace(category.Name) ? string.Empty : category.Name.Trim();
        category.Description = string.IsNullOrWhiteSpace(category.Description) ? null : category.Description.Trim();

        if (string.IsNullOrWhiteSpace(category.Code) || string.IsNullOrWhiteSpace(category.Name))
        {
            return (false, "Category code and name are required.");
        }

        var duplicate = await _db.ExpenseCategories.AnyAsync(c => c.CompanyId == existing.CompanyId && c.Code == category.Code && c.Id != category.Id);
        if (duplicate)
        {
            return (false, $"Category code '{category.Code}' already exists for the company.");
        }

        category.CompanyId = existing.CompanyId;
        category.CreatedAt = existing.CreatedAt;
        category.CreatedBy = existing.CreatedBy;
        _db.ExpenseCategories.Update(category);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var category = await _db.ExpenseCategories.FindAsync(id);
        if (category is null)
        {
            return (false, "Expense category not found.");
        }

        if (await _db.ExpenseTypes.AnyAsync(t => t.ExpenseCategoryId == id))
        {
            return (false, "Cannot delete a category that has expense types.");
        }

        _db.ExpenseCategories.Remove(category);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}