using CompanyERP.Entities.Sales;

namespace CompanyERP.Interfaces.Services;

public interface IServiceDeliveryService
{
    Task<List<ServiceDelivery>> GetAllAsync();
    Task<ServiceDelivery?> GetByIdAsync(int id);
    Task<(bool Success, string Error)> DeliverAsync(int companyId, int branchId, int serviceOrderId, DateTime deliveryDate, string? deliveredBy, decimal quantity, string? note);
}