using CompanyERP.Entities.Company;

namespace CompanyERP.Interfaces.Services;

public interface IAccountingPeriodService
{
    Task<List<AccountingPeriod>> GetAllAsync(int? financialYearId = null);
    Task<AccountingPeriod?> GetByIdAsync(int id);
    Task<List<FinancialYear>> GetFinancialYearsAsync();
    Task<FinancialYear?> GetFinancialYearAsync(int id);
    Task<bool> PeriodCodeExistsAsync(int financialYearId, string periodCode, int? excludeId = null);
    Task<bool> HasOverlapAsync(int financialYearId, DateTime startDate, DateTime endDate, int? excludeId = null);
    Task<(bool Success, string Error)> CreateAsync(AccountingPeriod period);
    Task<(bool Success, string Error)> UpdateAsync(AccountingPeriod period);
    Task<(bool Success, string Error)> CloseAsync(int id);
    Task<(bool Success, string Error)> ReopenAsync(int id);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}