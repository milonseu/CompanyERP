using CompanyERP.Entities.Purchase;

namespace CompanyERP.Interfaces.Services;

public interface IPurchaseReceivingService
{
    Task<List<PurchaseReceiving>> GetAllAsync();
    Task<PurchaseReceiving?> GetByIdAsync(int id);
    Task<(bool Success, string Error)> ReceiveAsync(int orderId, DateTime receivedDate, string? referenceNo, string? note);
}