using CompanyERP.Entities.Sales;

namespace CompanyERP.Interfaces.Services;

public interface IServiceOrderService
{
    Task<List<ServiceOrder>> GetAllAsync();
    Task<ServiceOrder?> GetByIdAsync(int id);
    Task<List<ServiceOrder>> GetDeliveryEligibleAsync();
    Task<string> GenerateNumberAsync(int companyId, DateTime orderDate);
    Task<(bool Success, string Error)> CreateAsync(ServiceOrder order);
    Task<(bool Success, string Error)> UpdateStatusAsync(int id, ServiceOrderStatus status);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}