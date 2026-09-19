using CompanyERP.Data;
using CompanyERP.Entities.CompanyBranch;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.CompanyBranch;

public class BranchService : IBranchService
{
    private readonly ApplicationDbContext _db;

    public BranchService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Branch>> GetAllAsync()
    {
        return await _db.Branches
            .Include(b => b.Company)
            .Include(b => b.BranchType)
            .Include(b => b.Settings)
            .OrderBy(b => b.Name)
            .ToListAsync();
    }

    public async Task<Branch?> GetByIdAsync(int id)
    {
        return await _db.Branches
            .Include(b => b.Company)
            .Include(b => b.BranchType)
            .Include(b => b.Settings)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<bool> CodeExistsAsync(int companyId, string code, int? excludeId = null)
    {
        var query = _db.Branches.Where(b => b.CompanyId == companyId && b.Code == code);
        if (excludeId.HasValue)
        {
            query = query.Where(b => b.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<bool> HeadOfficeExistsAsync(int companyId, int? excludeId = null)
    {
        var query = _db.Branches.Where(b => b.CompanyId == companyId && b.IsHeadOffice);
        if (excludeId.HasValue)
        {
            query = query.Where(b => b.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<(bool Success, string Error)> CreateAsync(Branch branch)
    {
        branch.Code = string.IsNullOrWhiteSpace(branch.Code) ? string.Empty : branch.Code.Trim().ToUpperInvariant();
        branch.Name = string.IsNullOrWhiteSpace(branch.Name) ? string.Empty : branch.Name.Trim();

        if (string.IsNullOrWhiteSpace(branch.Code) || string.IsNullOrWhiteSpace(branch.Name))
        {
            return (false, "Branch code and name are required.");
        }

        if (await CodeExistsAsync(branch.CompanyId, branch.Code))
        {
            return (false, "Branch code already exists for this company.");
        }

        if (branch.IsHeadOffice && await HeadOfficeExistsAsync(branch.CompanyId))
        {
            return (false, "This company already has a head office branch.");
        }

        branch.Settings ??= new BranchSettings();
        NormalizeSettings(branch.Settings);
        branch.Settings.Branch = branch;

        _db.Branches.Add(branch);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(Branch branch)
    {
        branch.Code = string.IsNullOrWhiteSpace(branch.Code) ? string.Empty : branch.Code.Trim().ToUpperInvariant();
        branch.Name = string.IsNullOrWhiteSpace(branch.Name) ? string.Empty : branch.Name.Trim();

        var existing = await _db.Branches
            .Include(b => b.Settings)
            .FirstOrDefaultAsync(b => b.Id == branch.Id);
        if (existing is null)
        {
            return (false, "Branch not found.");
        }

        if (await CodeExistsAsync(branch.CompanyId, branch.Code, branch.Id))
        {
            return (false, "Branch code already exists for this company.");
        }

        if (branch.IsHeadOffice && await HeadOfficeExistsAsync(branch.CompanyId, branch.Id))
        {
            return (false, "This company already has a head office branch.");
        }

        branch.CreatedAt = existing.CreatedAt;
        branch.CreatedBy = existing.CreatedBy;

        branch.Settings ??= new BranchSettings();
        NormalizeSettings(branch.Settings);

        if (existing.Settings is not null)
        {
            branch.Settings.Id = existing.Settings.Id;
            branch.Settings.CreatedAt = existing.Settings.CreatedAt;
            branch.Settings.CreatedBy = existing.Settings.CreatedBy;
        }

        branch.Settings.BranchId = branch.Id;
        branch.Settings.Branch = branch;

        _db.Branches.Update(branch);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateSettingsAsync(BranchSettings settings)
    {
        NormalizeSettings(settings);

        var existing = await _db.BranchSettings.FirstOrDefaultAsync(s => s.BranchId == settings.BranchId);
        if (existing is null)
        {
            return (false, "Branch settings not found.");
        }

        existing.TransactionPrefix = settings.TransactionPrefix;
        existing.DefaultCurrencyCode = settings.DefaultCurrencyCode;
        existing.DefaultPaymentDays = settings.DefaultPaymentDays;
        existing.AllowInventory = settings.AllowInventory;
        existing.AllowSales = settings.AllowSales;
        existing.AllowPurchase = settings.AllowPurchase;
        existing.AllowExpense = settings.AllowExpense;

        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var branch = await _db.Branches.FindAsync(id);
        if (branch is null)
        {
            return (false, "Branch not found.");
        }

        if (await _db.Warehouses.AnyAsync(w => w.BranchId == id))
        {
            return (false, "Branch cannot be deleted because warehouses exist under it.");
        }

        if (await _db.EmployeeBranchAssignments.AnyAsync(a => a.BranchId == id))
        {
            return (false, "Branch cannot be deleted because employees are assigned to it.");
        }

        _db.Branches.Remove(branch);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<Dictionary<int, (int Employees, int Warehouses)>> GetUsageCountsAsync()
    {
        var employees = await _db.EmployeeBranchAssignments
            .GroupBy(a => a.BranchId)
            .Select(g => new { BranchId = g.Key, Count = g.Count() })
            .ToListAsync();
        var warehouses = await _db.Warehouses
            .GroupBy(w => w.BranchId)
            .Select(g => new { BranchId = g.Key, Count = g.Count() })
            .ToListAsync();

        var result = new Dictionary<int, (int Employees, int Warehouses)>();
        foreach (var e in employees)
        {
            result[e.BranchId] = (e.Count, 0);
        }
        foreach (var w in warehouses)
        {
            result[w.BranchId] = (result.TryGetValue(w.BranchId, out var cur) ? cur.Employees : 0, w.Count);
        }
        return result;
    }

    private static void NormalizeSettings(BranchSettings settings)
    {
        settings.TransactionPrefix = string.IsNullOrWhiteSpace(settings.TransactionPrefix)
            ? null
            : settings.TransactionPrefix.Trim().ToUpperInvariant();
        settings.DefaultCurrencyCode = string.IsNullOrWhiteSpace(settings.DefaultCurrencyCode)
            ? "BDT"
            : settings.DefaultCurrencyCode.Trim().ToUpperInvariant();
    }
}