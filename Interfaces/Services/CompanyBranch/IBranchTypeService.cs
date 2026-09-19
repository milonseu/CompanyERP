using CompanyERP.Entities.CompanyBranch;

namespace CompanyERP.Interfaces.Services;

public interface IBranchTypeService
{
    Task<List<BranchType>> GetAllAsync();
    Task<BranchType?> GetByIdAsync(int id);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    Task<bool> HasBranchesAsync(int id);
    Task<(bool Success, string Error)> CreateAsync(BranchType branchType);
    Task<(bool Success, string Error)> UpdateAsync(BranchType branchType);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}