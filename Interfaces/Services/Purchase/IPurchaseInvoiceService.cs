using CompanyERP.Entities.Purchase;

namespace CompanyERP.Interfaces.Services;

public interface IPurchaseInvoiceService
{
    Task<List<PurchaseInvoice>> GetAllAsync();
    Task<PurchaseInvoice?> GetByIdAsync(int id);
    Task<List<PurchaseInvoice>> GetBySupplierIdAsync(int companyId, int supplierId);
    Task<(bool Success, string Error)> CreateFromOrderAsync(PurchaseOrder order, DateTime invoiceDate, DateTime? dueDate, string note);
}