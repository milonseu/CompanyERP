using CompanyERP.Entities.Payment;

namespace CompanyERP.Interfaces.Services;

public interface IBankAccountService
{
    Task<List<BankAccount>> GetAllAsync(int companyId);
    Task<BankAccount?> GetByIdAsync(int id);
    Task<bool> AccountNoExistsAsync(int companyId, string accountNo, int? excludeId = null);
    Task<(bool Success, string Error)> CreateAsync(BankAccount account);
    Task<(bool Success, string Error)> UpdateAsync(BankAccount account);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}