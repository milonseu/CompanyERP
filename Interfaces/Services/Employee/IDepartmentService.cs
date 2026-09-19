using CompanyERP.Entities.Employee;

namespace CompanyERP.Interfaces.Services;

public interface IDepartmentService
{
    Task<List<Department>> GetAllAsync();
    Task<List<Department>> GetByCompanyIdAsync(int companyId);
    Task<Department?> GetByIdAsync(int id);
    Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId = null);
    Task<bool> HasEmployeesAsync(int id);
    Task<(bool Success, string Error)> CreateAsync(Department department);
    Task<(bool Success, string Error)> UpdateAsync(Department department);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}