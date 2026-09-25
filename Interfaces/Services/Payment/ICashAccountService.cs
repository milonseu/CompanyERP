using CompanyERP.Entities.Payment;

namespace CompanyERP.Interfaces.Services;

public interface ICashAccountService
{
    Task<List<CashAccount>> GetAllAsync(int companyId);
    Task<CashAccount?> GetByIdAsync(int id);
    Task<bool> CodeExistsAsync(int companyId, string code, int? excludeId = null);
    Task<(bool Success, string Error)> CreateAsync(CashAccount account);
    Task<(bool Success, string Error)> UpdateAsync(CashAccount account);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}