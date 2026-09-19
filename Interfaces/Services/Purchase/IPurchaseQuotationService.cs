using CompanyERP.Entities.Purchase;

namespace CompanyERP.Interfaces.Services;

public interface IPurchaseQuotationService
{
    Task<List<PurchaseQuotation>> GetAllAsync();
    Task<PurchaseQuotation?> GetByIdAsync(int id);
    Task<string> GenerateNumberAsync(int companyId, DateTime quotationDate);
    Task<(bool Success, string Error)> CreateAsync(PurchaseQuotation quotation, List<PurchaseQuotationLine> lines);
    Task<(bool Success, string Error)> UpdateAsync(PurchaseQuotation quotation, List<PurchaseQuotationLine> lines);
    Task<(bool Success, string Error)> UpdateStatusAsync(int id, PurchaseQuotationStatus status);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}