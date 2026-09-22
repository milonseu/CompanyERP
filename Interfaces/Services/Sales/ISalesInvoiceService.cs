using CompanyERP.Entities.Sales;

namespace CompanyERP.Interfaces.Services;

public interface ISalesInvoiceService
{
    Task<List<SalesInvoice>> GetAllAsync();
    Task<SalesInvoice?> GetByIdAsync(int id);
    Task<List<SalesInvoice>> GetByCustomerIdAsync(int companyId, int customerId);
    Task<(bool Success, string Error)> CreateFromOrderAsync(int orderId, DateTime invoiceDate, DateTime? dueDate, int warehouseId, SalesPaymentType paymentType, decimal amountPaid, string? note);
    Task<(bool Success, string Error)> RecordPaymentAsync(int invoiceId, decimal amountPaid);
}