using CompanyERP.Entities.Sales;

namespace CompanyERP.Interfaces.Services;

public interface IServiceService
{
    Task<List<Service>> GetAllAsync();
    Task<List<Service>> GetByCompanyIdAsync(int companyId);
    Task<Service?> GetByIdAsync(int id);
    Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId = null);
    Task<(bool Success, string Error)> CreateAsync(Service service);
    Task<(bool Success, string Error)> UpdateAsync(Service service);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}