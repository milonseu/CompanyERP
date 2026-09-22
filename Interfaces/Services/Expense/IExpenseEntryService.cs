using CompanyERP.Entities.Expense;

namespace CompanyERP.Interfaces.Services;

public interface IExpenseEntryService
{
    Task<List<ExpenseEntry>> GetAllAsync(int companyId, int? branchId = null, int? typeId = null);
    Task<ExpenseEntry?> GetByIdAsync(int id);
    Task<string> GenerateExpenseNoAsync(int companyId, DateTime expenseDate);
    Task<(bool Success, string Error)> CreateAsync(ExpenseEntry entry);
    Task<(bool Success, string Error)> UpdateAsync(ExpenseEntry entry);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}