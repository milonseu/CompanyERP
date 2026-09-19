using CompanyERP.Entities.Purchase;

namespace CompanyERP.Interfaces.Services;

public interface IPurchaseReturnService
{
    Task<List<PurchaseReturn>> GetAllAsync();
    Task<PurchaseReturn?> GetByIdAsync(int id);
    Task<decimal> GetReturnedQuantityAsync(int invoiceId, int productId);
    Task<(bool Success, string Error)> CreateAsync(PurchaseReturn purchaseReturn, List<PurchaseReturnLine> lines);
}