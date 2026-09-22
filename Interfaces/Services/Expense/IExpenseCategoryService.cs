using CompanyERP.Entities.Expense;

namespace CompanyERP.Interfaces.Services;

public interface IExpenseCategoryService
{
    Task<List<ExpenseCategory>> GetAllAsync(int companyId);
    Task<ExpenseCategory?> GetByIdAsync(int id);
    Task<(bool Success, string Error)> CreateAsync(ExpenseCategory category);
    Task<(bool Success, string Error)> UpdateAsync(ExpenseCategory category);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}