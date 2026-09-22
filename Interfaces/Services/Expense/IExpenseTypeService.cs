using CompanyERP.Entities.Expense;

namespace CompanyERP.Interfaces.Services;

public interface IExpenseTypeService
{
    Task<List<ExpenseType>> GetAllAsync(int companyId);
    Task<ExpenseType?> GetByIdAsync(int id);
    Task<(bool Success, string Error)> CreateAsync(ExpenseType type);
    Task<(bool Success, string Error)> UpdateAsync(ExpenseType type);
    Task<(bool Success, string Error)> DeleteAsync(int id);
    Task<List<ExpenseType>> GetByCategoryAsync(int companyId, int categoryId);
}