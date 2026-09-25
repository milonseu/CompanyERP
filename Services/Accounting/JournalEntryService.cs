using CompanyERP.Data;
using CompanyERP.Entities.Accounting;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Accounting;

public class JournalEntryService : IJournalEntryService
{
    private readonly ApplicationDbContext _db;
    private readonly ITransactionPostingService _postingService;

    public JournalEntryService(ApplicationDbContext db, ITransactionPostingService postingService)
    {
        _db = db;
        _postingService = postingService;
    }

    public async Task<List<JournalEntry>> GetAllAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null, int? branchId = null)
    {
        var query = _db.JournalEntries
            .AsNoTracking()
            .Include(e => e.Company)
            .Include(e => e.Branch)
            .Where(e => e.CompanyId == companyId)
            .AsQueryable();

        if (branchId.HasValue)
        {
            query = query.Where(e => e.BranchId == branchId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.EntryDate.Date >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            query = query.Where(e => e.EntryDate.Date <= toDate.Value.Date);
        }

        return await query
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.Id)
            .ToListAsync();
    }

    public async Task<JournalEntry?> GetByIdAsync(int id)
    {
        return await _db.JournalEntries
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Include(e => e.Company)
            .Include(e => e.AccountingPeriod)
            .Include(e => e.Details).ThenInclude(d => d.Account)
            .FirstOrDefaultAsync();
    }

    public async Task<string> GenerateEntryNoAsync(int companyId, DateTime entryDate)
    {
        var count = await _db.JournalEntries
            .CountAsync(e => e.CompanyId == companyId && e.EntryDate.Date == entryDate.Date);
        return $"JR-{entryDate:yyyyMMdd}-{(count + 1):D3}";
    }

    public async Task<(bool Success, string Error)> CreateAsync(JournalEntry entry, List<JournalEntryDetail> details)
    {
        entry.Description = string.IsNullOrWhiteSpace(entry.Description) ? string.Empty : entry.Description.Trim();
        if (string.IsNullOrWhiteSpace(entry.Description))
        {
            return (false, "Description is required.");
        }

        var lines = details.Select(d => (d.AccountId, d.Debit, d.Credit, d.Note)).ToList();
        var result = await _postingService.PostManualAsync(entry.CompanyId, entry.EntryDate, entry.Description, lines);
        if (!result.Success)
        {
            return (false, result.Error);
        }

        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var entry = await _db.JournalEntries.FindAsync(id);
        if (entry is null)
        {
            return (false, "Journal entry not found.");
        }

        return (false, "Posted journal entries are financial records and cannot be deleted. Use a reversing entry instead.");
    }
}