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

    public async Task<List<ChartOfAccount>> GetPostableAsync(int companyId)
    {
        return await _db.ChartOfAccounts
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.IsActive && a.IsPostable)
            .OrderBy(a => a.AccountType)
            .ThenBy(a => a.AccountCode)
            .ToListAsync();
    }

    public async Task<List<(ChartOfAccount Account, int Depth)>> GetTreeAsync(int companyId)
    {
        var accounts = await GetAllAsync(companyId);
        var idSet = accounts.Select(a => a.Id).ToHashSet();
        var children = accounts
            .Where(a => a.ParentId.HasValue)
            .GroupBy(a => a.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(a => a.AccountCode).ToList());

        var appended = new HashSet<int>();
        var result = new List<(ChartOfAccount Account, int Depth)>();

        foreach (var root in accounts
                     .Where(a => !a.ParentId.HasValue || !idSet.Contains(a.ParentId!.Value))
                     .OrderBy(a => a.AccountCode))
        {
            AppendTree(root, 0);
        }

        return result;

        void AppendTree(ChartOfAccount node, int depth)
        {
            if (!appended.Add(node.Id))
            {
                return;
            }

            result.Add((node, depth));
            if (children.TryGetValue(node.Id, out var list))
            {
                foreach (var child in list)
                {
                    AppendTree(child, depth + 1);
                }
            }
        }
    }

    public async Task<List<ChartOfAccount>> GetParentCandidatesAsync(int companyId, int? excludeId = null)
    {
        var accounts = await _db.ChartOfAccounts
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.IsActive)
            .ToListAsync();

        if (!excludeId.HasValue)
        {
            return accounts.OrderBy(a => a.AccountType).ThenBy(a => a.AccountCode).ToList();
        }

        // Exclude the account itself and all of its descendants to prevent cycles.
        var excluded = new HashSet<int>();
        Collect(excludeId.Value);

        return accounts
            .Where(a => a.Id != excludeId.Value && !excluded.Contains(a.Id))
            .OrderBy(a => a.AccountType)
            .ThenBy(a => a.AccountCode)
            .ToList();

        void Collect(int id)
        {
            foreach (var child in accounts.Where(a => a.ParentId.HasValue && a.ParentId.Value == id))
            {
                if (excluded.Add(child.Id))
                {
                    Collect(child.Id);
                }
            }
        }
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

        var parent = await ValidateParentAsync(account);
        if (parent.Success != true)
        {
            return (false, parent.Error);
        }

        if (account.OpeningBalance != 0)
        {
            account.OpeningBalance = account.NormalBalance == AccountNormalBalance.Debit
                ? Math.Abs(account.OpeningBalance)
                : -Math.Abs(account.OpeningBalance);
        }

        _db.ChartOfAccounts.Add(account);
        await _db.SaveChangesAsync();
        await RefreshParentFlagsAsync(account.ParentId);
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

        var hasChildren = await _db.ChartOfAccounts.AnyAsync(a => a.ParentId == account.Id);
        if (hasChildren)
        {
            return (false, "This account is a parent (has child accounts) and cannot be modified. Edit the child accounts instead.");
        }

        if (await CodeExistsAsync(account.AccountCode, account.CompanyId, account.Id))
        {
            return (false, $"Account code '{account.AccountCode}' already exists for the company.");
        }

        if (await _db.JournalEntryDetails.AnyAsync(d => d.AccountId == account.Id))
        {
            return (false, "Accounts with posted activity cannot be modified.");
        }

        var parent = await ValidateParentAsync(account);
        if (parent.Success != true)
        {
            return (false, parent.Error);
        }

        var oldParentId = existing.ParentId;
        account.CreatedAt = existing.CreatedAt;
        account.CreatedBy = existing.CreatedBy;

        _db.ChartOfAccounts.Update(account);
        await _db.SaveChangesAsync();

        await RefreshParentFlagsAsync(account.ParentId);
        await RefreshParentFlagsAsync(oldParentId);
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var account = await _db.ChartOfAccounts.FindAsync(id);
        if (account is null)
        {
            return (false, "Chart of Account entry not found.");
        }

        if (await _db.ChartOfAccounts.AnyAsync(a => a.ParentId == id))
        {
            return (false, "Parent accounts with child accounts cannot be deleted. Remove the child accounts first.");
        }

        if (await _db.JournalEntryDetails.AnyAsync(d => d.AccountId == id))
        {
            return (false, "Accounts with posted journal activity cannot be deleted.");
        }

        var oldParentId = account.ParentId;
        _db.ChartOfAccounts.Remove(account);
        await _db.SaveChangesAsync();

        await RefreshParentFlagsAsync(oldParentId);
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> EnsureDefaultsAsync(int companyId)
    {
        return await Task.FromResult((true, string.Empty));
    }

    private async Task<(bool? Success, string Error)> ValidateParentAsync(ChartOfAccount account)
    {
        if (!account.ParentId.HasValue)
        {
            return (true, string.Empty);
        }

        if (account.ParentId.Value == account.Id)
        {
            return (false, "An account cannot be its own parent.");
        }

        var parent = await _db.ChartOfAccounts.FirstOrDefaultAsync(a => a.Id == account.ParentId.Value);
        if (parent is null)
        {
            return (false, "Selected parent account does not exist.");
        }

        if (parent.CompanyId != account.CompanyId)
        {
            return (false, "Parent account must belong to the same company.");
        }

        if (!parent.IsActive)
        {
            return (false, "Parent account is inactive.");
        }

        if (parent.OpeningBalance != 0)
        {
            return (false, "An account with an opening balance cannot be used as a parent account.");
        }

        if (parent.AccountType != account.AccountType)
        {
            return (false, "Account type must match the parent's class.");
        }

        return (true, string.Empty);
    }

    private async Task RefreshParentFlagsAsync(int? parentId)
    {
        if (!parentId.HasValue)
        {
            return;
        }

        var parent = await _db.ChartOfAccounts.FindAsync(parentId.Value);
        if (parent is null)
        {
            return;
        }

        var hasChildren = await _db.ChartOfAccounts.AnyAsync(a => a.ParentId == parent.Id);
        parent.IsPostable = !hasChildren;
        parent.IsLeaf = !hasChildren;
        await _db.SaveChangesAsync();
    }
}