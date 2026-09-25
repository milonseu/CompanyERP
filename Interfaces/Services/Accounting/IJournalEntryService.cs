using CompanyERP.Entities.Accounting;

namespace CompanyERP.Interfaces.Services;

public interface IJournalEntryService
{
    Task<List<JournalEntry>> GetAllAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null, int? branchId = null);
    Task<JournalEntry?> GetByIdAsync(int id);
    Task<string> GenerateEntryNoAsync(int companyId, DateTime entryDate);
    Task<(bool Success, string Error)> CreateAsync(JournalEntry entry, List<JournalEntryDetail> details);
    Task<(bool Success, string Error)> DeleteAsync(int id);
}