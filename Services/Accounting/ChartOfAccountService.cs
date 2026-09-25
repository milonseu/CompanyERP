using CompanyERP.Data;
using CompanyERP.Entities.Accounting;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Accounting;

public class ChartOfAccountService : IChartOfAccountService
{
    private readonly ApplicationDbContext _db;

    public ChartOfAccountService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<ChartOfAccount>> GetAllAsync(int companyId)
    {
        return await _db.ChartOfAccounts
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId)
            .OrderBy(a => a.AccountCode)
            .ToListAsync();
    }

    public async Task<List<ChartOfAccount>> GetActiveAsync(int companyId)
    {
        return await _db.ChartOfAccounts
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.IsActive)
            .OrderBy(a => a.AccountType)
            .ThenBy(a => a.AccountCode)
            .ToListAsync();
    }

    public async Task<ChartOfAccount?> GetByIdAsync(int id)
    {
        return await _db.ChartOfAccounts
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Include(a => a.Company)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId = null)
    {
        var query = _db.ChartOfAccounts.Where(a => a.AccountCode == code && a.CompanyId == companyId);
        if (excludeId.HasValue)
        {
            query = query.Where(a => a.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<(bool Success, string Error)> CreateAsync(ChartOfAccount account)
    {
        account.AccountCode = string.IsNullOrWhiteSpace(account.AccountCode) ? string.Empty : account.AccountCode.Trim();
        account.AccountName = string.IsNullOrWhiteSpace(account.AccountName) ? string.Empty : account.AccountName.Trim();
        account.Description = string.IsNullOrWhiteSpace(account.Description) ? null : account.Description.Trim();

        if (string.IsNullOrWhiteSpace(account.AccountCode) || string.IsNullOrWhiteSpace(account.AccountName))
        {
            return (false, "Account code and account name are required.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == account.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (await CodeExistsAsync(account.AccountCode, account.CompanyId))
        {
            return (false, $"Account code '{account.AccountCode}' already exists for the company.");
        }

        if (account.OpeningBalance != 0)
        {
            account.OpeningBalance = account.NormalBalance == AccountNormalBalance.Debit
                ? Math.Abs(account.OpeningBalance)
                : -Math.Abs(account.OpeningBalance);
        }

        _db.ChartOfAccounts.Add(account);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(ChartOfAccount account)
    {
        account.AccountCode = string.IsNullOrWhiteSpace(account.AccountCode) ? string.Empty : account.AccountCode.Trim();
        account.AccountName = string.IsNullOrWhiteSpace(account.AccountName) ? string.Empty : account.AccountName.Trim();
        account.Description = string.IsNullOrWhiteSpace(account.Description) ? null : account.Description.Trim();

        var existing = await _db.ChartOfAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == account.Id);
        if (existing is null)
        {
            return (false, "Chart of Account entry not found.");
        }

        if (await CodeExistsAsync(account.AccountCode, account.CompanyId, account.Id))
        {
            return (false, $"Account code '{account.AccountCode}' already exists for the company.");
        }

        if (await _db.JournalEntryDetails.AnyAsync(d => d.AccountId == account.Id))
        {
            return (false, "Accounts with posted activity cannot be modified.");
        }

        account.CreatedAt = existing.CreatedAt;
        account.CreatedBy = existing.CreatedBy;

        _db.ChartOfAccounts.Update(account);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var account = await _db.ChartOfAccounts.FindAsync(id);
        if (account is null)
        {
            return (false, "Chart of Account entry not found.");
        }

        if (await _db.JournalEntryDetails.AnyAsync(d => d.AccountId == id))
        {
            return (false, "Accounts with posted journal activity cannot be deleted.");
        }

        _db.ChartOfAccounts.Remove(account);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> EnsureDefaultsAsync(int companyId)
    {
        return await Task.FromResult((true, string.Empty));
    }
}