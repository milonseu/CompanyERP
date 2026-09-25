using CompanyERP.Data;
using CompanyERP.Entities.Payment;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Payments;

public class BankAccountService : IBankAccountService
{
    private readonly ApplicationDbContext _db;

    public BankAccountService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<BankAccount>> GetAllAsync(int companyId)
    {
        return await _db.BankAccounts
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId)
            .Include(a => a.Company)
            .Include(a => a.Branch)
            .OrderBy(a => a.AccountNo)
            .ToListAsync();
    }

    public async Task<BankAccount?> GetByIdAsync(int id)
    {
        return await _db.BankAccounts
            .AsNoTracking()
            .Include(a => a.Company)
            .Include(a => a.Branch)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<bool> AccountNoExistsAsync(int companyId, string accountNo, int? excludeId = null)
    {
        return await _db.BankAccounts.AnyAsync(a =>
            a.CompanyId == companyId && a.AccountNo == accountNo && (!excludeId.HasValue || a.Id != excludeId.Value));
    }

    public async Task<(bool Success, string Error)> CreateAsync(BankAccount account)
    {
        account.BankName = string.IsNullOrWhiteSpace(account.BankName) ? string.Empty : account.BankName.Trim();
        account.AccountName = string.IsNullOrWhiteSpace(account.AccountName) ? string.Empty : account.AccountName.Trim();
        account.AccountNo = string.IsNullOrWhiteSpace(account.AccountNo) ? string.Empty : account.AccountNo.Trim();
        account.Note = string.IsNullOrWhiteSpace(account.Note) ? null : account.Note.Trim();

        if (string.IsNullOrWhiteSpace(account.BankName) || string.IsNullOrWhiteSpace(account.AccountName) || string.IsNullOrWhiteSpace(account.AccountNo))
        {
            return (false, "Bank name, account name and account number are required.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == account.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (!await _db.Branches.AnyAsync(b => b.Id == account.BranchId && b.CompanyId == account.CompanyId))
        {
            return (false, "Selected branch does not belong to the company.");
        }

        if (await AccountNoExistsAsync(account.CompanyId, account.AccountNo))
        {
            return (false, $"Bank account number '{account.AccountNo}' already exists for the company.");
        }

        _db.BankAccounts.Add(account);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(BankAccount account)
    {
        var existing = await _db.BankAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == account.Id);
        if (existing is null)
        {
            return (false, "Bank account not found.");
        }

        account.BankName = string.IsNullOrWhiteSpace(account.BankName) ? string.Empty : account.BankName.Trim();
        account.AccountName = string.IsNullOrWhiteSpace(account.AccountName) ? string.Empty : account.AccountName.Trim();
        account.AccountNo = string.IsNullOrWhiteSpace(account.AccountNo) ? string.Empty : account.AccountNo.Trim();
        account.Note = string.IsNullOrWhiteSpace(account.Note) ? null : account.Note.Trim();

        if (string.IsNullOrWhiteSpace(account.BankName) || string.IsNullOrWhiteSpace(account.AccountName) || string.IsNullOrWhiteSpace(account.AccountNo))
        {
            return (false, "Bank name, account name and account number are required.");
        }

        if (await AccountNoExistsAsync(existing.CompanyId, account.AccountNo, account.Id))
        {
            return (false, $"Bank account number '{account.AccountNo}' already exists for the company.");
        }

        account.CompanyId = existing.CompanyId;
        account.CreatedAt = existing.CreatedAt;
        account.CreatedBy = existing.CreatedBy;
        _db.BankAccounts.Update(account);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var account = await _db.BankAccounts.FindAsync(id);
        if (account is null)
        {
            return (false, "Bank account not found.");
        }

        if (await _db.Payments.AnyAsync(p => p.BankAccountId == id))
        {
            return (false, "Bank account is used by payments and cannot be deleted.");
        }

        _db.BankAccounts.Remove(account);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}