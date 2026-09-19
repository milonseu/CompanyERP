using CompanyERP.Data;
using CompanyERP.Entities.CompanyBranch;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.CompanyBranch;

public class BranchTypeService : IBranchTypeService
{
    private readonly ApplicationDbContext _db;

    public BranchTypeService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<BranchType>> GetAllAsync()
    {
        return await _db.BranchTypes
            .OrderBy(bt => bt.Name)
            .ToListAsync();
    }

    public async Task<BranchType?> GetByIdAsync(int id)
    {
        return await _db.BranchTypes.FindAsync(id);
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
    {
        var query = _db.BranchTypes.Where(bt => bt.Code == code);
        if (excludeId.HasValue)
        {
            query = query.Where(bt => bt.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<bool> HasBranchesAsync(int id)
    {
        return await _db.Branches.AnyAsync(b => b.BranchTypeId == id);
    }

    public async Task<(bool Success, string Error)> CreateAsync(BranchType branchType)
    {
        branchType.Code = string.IsNullOrWhiteSpace(branchType.Code) ? string.Empty : branchType.Code.Trim().ToUpperInvariant();
        branchType.Name = string.IsNullOrWhiteSpace(branchType.Name) ? string.Empty : branchType.Name.Trim();

        if (string.IsNullOrWhiteSpace(branchType.Code) || string.IsNullOrWhiteSpace(branchType.Name))
        {
            return (false, "Branch type code and name are required.");
        }

        if (await CodeExistsAsync(branchType.Code))
        {
            return (false, "Branch type code already exists.");
        }

        _db.BranchTypes.Add(branchType);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(BranchType branchType)
    {
        branchType.Code = string.IsNullOrWhiteSpace(branchType.Code) ? string.Empty : branchType.Code.Trim().ToUpperInvariant();
        branchType.Name = string.IsNullOrWhiteSpace(branchType.Name) ? string.Empty : branchType.Name.Trim();

        var existing = await _db.BranchTypes.AsNoTracking().FirstOrDefaultAsync(bt => bt.Id == branchType.Id);
        if (existing is null)
        {
            return (false, "Branch type not found.");
        }

        if (await CodeExistsAsync(branchType.Code, branchType.Id))
        {
            return (false, "Branch type code already exists.");
        }

        branchType.CreatedAt = existing.CreatedAt;
        branchType.CreatedBy = existing.CreatedBy;

        _db.BranchTypes.Update(branchType);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var branchType = await _db.BranchTypes.FindAsync(id);
        if (branchType is null)
        {
            return (false, "Branch type not found.");
        }

        if (await HasBranchesAsync(id))
        {
            return (false, "Branch type cannot be deleted because it is used by one or more branches.");
        }

        _db.BranchTypes.Remove(branchType);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}