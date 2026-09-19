using CompanyERP.Entities.Employee;

namespace CompanyERP.Interfaces.Services;

public interface IDesignationService
{
    Task<List<Designation>> GetAllAsync();
    Task<List<Designation>> GetByCompanyIdAsync(int companyId);
    Task<Designation?> GetByIdAsync(int id);
    Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId = null);
    Task<bool> HasEmployeesAsync(int id);
    Task<(bool Success, string Error)> CreateAsync(Designation designation);
    Task<(bool Success, string Error)> UpdateAsync(Designation designation);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}