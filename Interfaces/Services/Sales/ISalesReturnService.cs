using CompanyERP.Entities.Sales;

namespace CompanyERP.Interfaces.Services;

public interface ISalesReturnService
{
    Task<List<SalesReturn>> GetAllAsync();
    Task<SalesReturn?> GetByIdAsync(int id);
    Task<Dictionary<(SalesItemType ItemType, int? ProductId, int? ServiceId), decimal>> GetReturnedByInvoiceAsync(int invoiceId);
    Task<(bool Success, string Error)> CreateFromInvoiceAsync(int invoiceId, DateTime returnDate, List<SalesReturnLine> lines, string? note);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}