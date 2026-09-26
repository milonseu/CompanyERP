using CompanyERP.ViewModels.Reports;

namespace CompanyERP.Interfaces.Services;

public interface IExpenseReportService
{
    Task<ExpenseSummaryViewModel> GetSummaryAsync(int companyId, DateTime? fromDate, DateTime? toDate, int? expenseTypeId);
    Task<ExpenseTrendViewModel> GetTrendAsync(int companyId);
    Task<ExpenseRegisterViewModel> GetRegisterAsync(int companyId, DateTime? fromDate, DateTime? toDate, int? branchId, int? supplierId);
}