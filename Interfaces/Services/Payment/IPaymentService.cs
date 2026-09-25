using CompanyERP.Entities.Employee;
using CompanyERP.Entities.Payment;

namespace CompanyERP.Interfaces.Services;

public interface IPaymentService
{
    Task<List<Payment>> GetAllAsync(int companyId, PaymentCategory? category = null);
    Task<Payment?> GetByIdAsync(int id);
    Task<string> GeneratePaymentNoAsync(int companyId, DateTime paymentDate);

    Task<List<SalaryPayment>> GetPendingSalaryCandidatesAsync(int companyId);

    Task<decimal> GetCustomerOutstandingAsync(int customerId);
    Task<decimal> GetSupplierOutstandingAsync(int supplierId);
    Task<decimal> GetExpenseOutstandingAsync(int expenseEntryId);
    Task<decimal> GetAssetOutstandingAsync(int assetRegisterId);

    Task<(bool Success, string Error)> CreateAsync(Payment payment);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}