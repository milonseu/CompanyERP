using CompanyERP.Data;
using CompanyERP.Entities.Expense;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Expense;

public class ExpenseEntryService : IExpenseEntryService
{
    private readonly ApplicationDbContext _db;
    private readonly ITransactionPostingService _postingService;

    public ExpenseEntryService(ApplicationDbContext db, ITransactionPostingService postingService)
    {
        _db = db;
        _postingService = postingService;
    }

    public async Task<List<ExpenseEntry>> GetAllAsync(int companyId, int? branchId = null, int? typeId = null)
    {
        var query = _db.ExpenseEntries
            .AsNoTracking()
            .Where(e => e.CompanyId == companyId)
            .Include(e => e.Branch)
            .Include(e => e.ExpenseType).ThenInclude(t => t!.ExpenseCategory)
            .AsQueryable();

        if (branchId.HasValue)
        {
            query = query.Where(e => e.BranchId == branchId.Value);
        }

        if (typeId.HasValue)
        {
            query = query.Where(e => e.ExpenseTypeId == typeId.Value);
        }

        return await query
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.Id)
            .ToListAsync();
    }

    public async Task<ExpenseEntry?> GetByIdAsync(int id)
    {
        return await _db.ExpenseEntries
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Include(e => e.Company)
            .Include(e => e.Branch)
            .Include(e => e.ExpenseType).ThenInclude(t => t!.ExpenseCategory)
            .Include(e => e.Supplier)
            .FirstOrDefaultAsync();
    }

    public async Task<string> GenerateExpenseNoAsync(int companyId, DateTime expenseDate)
    {
        var count = await _db.ExpenseEntries
            .CountAsync(e => e.CompanyId == companyId && e.ExpenseDate.Date == expenseDate.Date);
        return $"EXP-{expenseDate:yyyyMMdd}-{(count + 1):D3}";
    }

    public async Task<(bool Success, string Error)> CreateAsync(ExpenseEntry entry)
    {
        entry.ExpenseNo = string.IsNullOrWhiteSpace(entry.ExpenseNo) ? string.Empty : entry.ExpenseNo.Trim();
        entry.Description = string.IsNullOrWhiteSpace(entry.Description) ? string.Empty : entry.Description.Trim();
        entry.PaymentReference = string.IsNullOrWhiteSpace(entry.PaymentReference) ? null : entry.PaymentReference.Trim();

        if (string.IsNullOrWhiteSpace(entry.ExpenseNo))
        {
            return (false, "Expense number is required.");
        }

        if (string.IsNullOrWhiteSpace(entry.Description))
        {
            return (false, "Description is required.");
        }

        if (entry.Amount <= 0)
        {
            return (false, "Amount must be greater than zero.");
        }

        if (entry.AmountPaid > entry.Amount)
        {
            return (false, "Amount paid cannot exceed the expense amount.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == entry.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (!await _db.Branches.AnyAsync(b => b.Id == entry.BranchId && b.CompanyId == entry.CompanyId))
        {
            return (false, "Selected branch does not belong to the company.");
        }

        if (!await _db.ExpenseTypes.AnyAsync(t => t.Id == entry.ExpenseTypeId && t.CompanyId == entry.CompanyId))
        {
            return (false, "Selected expense type does not belong to the company.");
        }

        if (entry.SupplierId.HasValue &&
            !await _db.Suppliers.AnyAsync(s => s.Id == entry.SupplierId.Value && s.CompanyId == entry.CompanyId))
        {
            return (false, "Selected supplier does not belong to the company.");
        }

        if (await _db.ExpenseEntries.AnyAsync(e => e.CompanyId == entry.CompanyId && e.ExpenseNo == entry.ExpenseNo))
        {
            return (false, $"Expense number '{entry.ExpenseNo}' already exists for the company.");
        }

        entry.ExpenseDate = entry.ExpenseDate == default ? DateTime.Today : entry.ExpenseDate;

        // NOTE: Accounting effect is created during Accounting module integration:
        // Debit Operating Expense, Credit Cash/Bank or Expense Payable.
        var post = await _postingService.PostExpenseEntryAsync(entry);
        if (!post.Success)
        {
            return (false, post.Error);
        }

        _db.ExpenseEntries.Add(entry);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(ExpenseEntry entry)
    {
        var existing = await _db.ExpenseEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == entry.Id);
        if (existing is null)
        {
            return (false, "Expense entry not found.");
        }

        entry.ExpenseNo = string.IsNullOrWhiteSpace(entry.ExpenseNo) ? string.Empty : entry.ExpenseNo.Trim();
        entry.Description = string.IsNullOrWhiteSpace(entry.Description) ? string.Empty : entry.Description.Trim();
        entry.PaymentReference = string.IsNullOrWhiteSpace(entry.PaymentReference) ? null : entry.PaymentReference.Trim();

        if (string.IsNullOrWhiteSpace(entry.ExpenseNo) || string.IsNullOrWhiteSpace(entry.Description))
        {
            return (false, "Expense number and description are required.");
        }

        if (entry.Amount <= 0)
        {
            return (false, "Amount must be greater than zero.");
        }

        if (entry.AmountPaid > entry.Amount)
        {
            return (false, "Amount paid cannot exceed the expense amount.");
        }

        if (await _db.ExpenseEntries.AnyAsync(e => e.CompanyId == existing.CompanyId && e.ExpenseNo == entry.ExpenseNo && e.Id != entry.Id))
        {
            return (false, $"Expense number '{entry.ExpenseNo}' already exists for the company.");
        }

        if (!await _db.ExpenseTypes.AnyAsync(t => t.Id == entry.ExpenseTypeId && t.CompanyId == existing.CompanyId))
        {
            return (false, "Selected expense type does not belong to the company.");
        }

        entry.CompanyId = existing.CompanyId;
        entry.CreatedAt = existing.CreatedAt;
        entry.CreatedBy = existing.CreatedBy;
        _db.ExpenseEntries.Update(entry);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var entry = await _db.ExpenseEntries.FindAsync(id);
        if (entry is null)
        {
            return (false, "Expense entry not found.");
        }

        return (false, "Posted expense entries are financial records and cannot be deleted. Use an adjustment entry instead.");
    }
}