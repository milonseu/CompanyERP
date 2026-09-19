using CompanyERP.Entities.Company;

namespace CompanyERP.Interfaces.Services;

public interface ICompanyProfileService
{
    Task<List<CompanyProfile>> GetAllAsync();
    Task<CompanyProfile?> GetByIdAsync(int id);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    Task<(bool Success, string Error)> CreateAsync(CompanyProfile profile);
    Task<(bool Success, string Error)> UpdateAsync(CompanyProfile profile);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}