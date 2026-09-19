using CompanyERP.Entities.CompanyBranch;

namespace CompanyERP.Interfaces.Services;

public interface IBranchService
{
    Task<List<Branch>> GetAllAsync();
    Task<Branch?> GetByIdAsync(int id);
    Task<bool> CodeExistsAsync(int companyId, string code, int? excludeId = null);
    Task<bool> HeadOfficeExistsAsync(int companyId, int? excludeId = null);
    Task<(bool Success, string Error)> CreateAsync(Branch branch);
    Task<(bool Success, string Error)> UpdateAsync(Branch branch);
    Task<(bool Success, string Error)> UpdateSettingsAsync(BranchSettings settings);
    Task<(bool Success, string Error)> DeleteAsync(int id);
    Task<Dictionary<int, (int Employees, int Warehouses)>> GetUsageCountsAsync();
}