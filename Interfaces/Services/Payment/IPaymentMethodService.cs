using CompanyERP.Entities.Payment;

namespace CompanyERP.Interfaces.Services;

public interface IPaymentMethodService
{
    Task<List<PaymentMethod>> GetAllAsync(int companyId);
    Task<PaymentMethod?> GetByIdAsync(int id);
    Task<bool> CodeExistsAsync(int companyId, string code, int? excludeId = null);
    Task<(bool Success, string Error)> CreateAsync(PaymentMethod method);
    Task<(bool Success, string Error)> UpdateAsync(PaymentMethod method);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}