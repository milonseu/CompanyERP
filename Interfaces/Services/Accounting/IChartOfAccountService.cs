using CompanyERP.Entities.Accounting;

namespace CompanyERP.Interfaces.Services;

public interface IChartOfAccountService
{
    Task<List<ChartOfAccount>> GetAllAsync(int companyId);
    Task<List<ChartOfAccount>> GetActiveAsync(int companyId);
    Task<ChartOfAccount?> GetByIdAsync(int id);
    Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId = null);
    Task<(bool Success, string Error)> CreateAsync(ChartOfAccount account);
    Task<(bool Success, string Error)> UpdateAsync(ChartOfAccount account);
    Task<(bool Success, string Error)> DeleteAsync(int id);
    Task<(bool Success, string Error)> EnsureDefaultsAsync(int companyId);
}