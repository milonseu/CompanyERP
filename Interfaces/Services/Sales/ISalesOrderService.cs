using CompanyERP.Entities.Sales;

namespace CompanyERP.Interfaces.Services;

public interface ISalesOrderService
{
    Task<List<SalesOrder>> GetAllAsync();
    Task<SalesOrder?> GetByIdAsync(int id);
    Task<List<SalesOrder>> GetConfirmedOrdersAsync();
    Task<string> GenerateNumberAsync(int companyId, DateTime orderDate);
    Task<(bool Success, string Error)> CreateAsync(SalesOrder order, List<SalesOrderLine> lines);
    Task<(bool Success, string Error)> UpdateAsync(SalesOrder order, List<SalesOrderLine> lines);
    Task<(bool Success, string Error)> UpdateStatusAsync(int id, SalesOrderStatus status);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}