using CompanyERP.Entities.Company;

namespace CompanyERP.Interfaces.Services;

public interface IFinancialYearService
{
    Task<List<FinancialYear>> GetAllAsync();
    Task<FinancialYear?> GetByIdAsync(int id);
    Task<bool> YearCodeExistsAsync(string yearCode, int? excludeId = null);
    Task<bool> HasOverlapAsync(DateTime startDate, DateTime endDate, int? excludeId = null);
    Task<(bool Success, string Error)> CreateAsync(FinancialYear year);
    Task<(bool Success, string Error)> UpdateAsync(FinancialYear year);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}