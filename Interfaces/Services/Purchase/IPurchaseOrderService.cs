using CompanyERP.Entities.Purchase;

namespace CompanyERP.Interfaces.Services;

public interface IPurchaseOrderService
{
    Task<List<PurchaseOrder>> GetAllAsync();
    Task<PurchaseOrder?> GetByIdAsync(int id);
    Task<List<PurchaseOrder>> GetReceivableOrdersAsync();
    Task<string> GenerateNumberAsync(int companyId, DateTime orderDate);
    Task<(bool Success, string Error)> CreateAsync(PurchaseOrder order, List<PurchaseOrderLine> lines);
    Task<(bool Success, string Error)> UpdateAsync(PurchaseOrder order, List<PurchaseOrderLine> lines);
    Task<(bool Success, string Error)> UpdateStatusAsync(int id, PurchaseOrderStatus status, int? quotationId = null);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}