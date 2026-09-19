using CompanyERP.Entities.Purchase;

namespace CompanyERP.Interfaces.Services;

public interface IPurchaseRequestService
{
    Task<List<PurchaseRequest>> GetAllAsync();
    Task<PurchaseRequest?> GetByIdAsync(int id);
    Task<bool> NumberExistsAsync(string requestNo, int companyId, int? excludeId = null);
    Task<string> GenerateNumberAsync(int companyId, DateTime requestDate);
    Task<(bool Success, string Error)> CreateAsync(PurchaseRequest request, List<PurchaseRequestLine> lines);
    Task<(bool Success, string Error)> UpdateAsync(PurchaseRequest request, List<PurchaseRequestLine> lines);
    Task<(bool Success, string Error)> UpdateStatusAsync(int id, PurchaseRequestStatus status);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}