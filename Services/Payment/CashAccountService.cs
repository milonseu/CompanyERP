using CompanyERP.Data;
using CompanyERP.Entities.Payment;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Payments;

public class CashAccountService : ICashAccountService
{
    private readonly ApplicationDbContext _db;

    public CashAccountService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<CashAccount>> GetAllAsync(int companyId)
    {
        return await _db.CashAccounts
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId)
            .Include(a => a.Company)
            .Include(a => a.Branch)
            .OrderBy(a => a.AccountCode)
            .ToListAsync();
    }

    public async Task<CashAccount?> GetByIdAsync(int id)
    {
        return await _db.CashAccounts
            .AsNoTracking()
            .Include(a => a.Company)
            .Include(a => a.Branch)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<bool> CodeExistsAsync(int companyId, string code, int? excludeId = null)
    {
        return await _db.CashAccounts.AnyAsync(a =>
            a.CompanyId == companyId && a.AccountCode == code && (!excludeId.HasValue || a.Id != excludeId.Value));
    }

    public async Task<(bool Success, string Error)> CreateAsync(CashAccount account)
    {
        account.AccountName = string.IsNullOrWhiteSpace(account.AccountName) ? string.Empty : account.AccountName.Trim();
        account.AccountCode = string.IsNullOrWhiteSpace(account.AccountCode) ? string.Empty : account.AccountCode.Trim();
        account.Note = string.IsNullOrWhiteSpace(account.Note) ? null : account.Note.Trim();

        if (string.IsNullOrWhiteSpace(account.AccountName) || string.IsNullOrWhiteSpace(account.AccountCode))
        {
            return (false, "Account name and account code are required.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == account.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (!await _db.Branches.AnyAsync(b => b.Id == account.BranchId && b.CompanyId == account.CompanyId))
        {
            return (false, "Selected branch does not belong to the company.");
        }

        if (await CodeExistsAsync(account.CompanyId, account.AccountCode))
        {
            return (false, $"Cash account code '{account.AccountCode}' already exists for the company.");
        }

        _db.CashAccounts.Add(account);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(CashAccount account)
    {
        var existing = await _db.CashAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == account.Id);
        if (existing is null)
        {
            return (false, "Cash account not found.");
        }

        account.AccountName = string.IsNullOrWhiteSpace(account.AccountName) ? string.Empty : account.AccountName.Trim();
        account.AccountCode = string.IsNullOrWhiteSpace(account.AccountCode) ? string.Empty : account.AccountCode.Trim();
        account.Note = string.IsNullOrWhiteSpace(account.Note) ? null : account.Note.Trim();

        if (string.IsNullOrWhiteSpace(account.AccountName) || string.IsNullOrWhiteSpace(account.AccountCode))
        {
            return (false, "Account name and account code are required.");
        }

        if (await CodeExistsAsync(existing.CompanyId, account.AccountCode, account.Id))
        {
            return (false, $"Cash account code '{account.AccountCode}' already exists for the company.");
        }

        account.CompanyId = existing.CompanyId;
        account.CreatedAt = existing.CreatedAt;
        account.CreatedBy = existing.CreatedBy;
        _db.CashAccounts.Update(account);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var account = await _db.CashAccounts.FindAsync(id);
        if (account is null)
        {
            return (false, "Cash account not found.");
        }

        if (await _db.Payments.AnyAsync(p => p.CashAccountId == id))
        {
            return (false, "Cash account is used by payments and cannot be deleted.");
        }

        _db.CashAccounts.Remove(account);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}