using CompanyERP.Data;
using CompanyERP.Entities.Company;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Company;

public class CompanyProfileService : ICompanyProfileService
{
    private readonly ApplicationDbContext _db;

    public CompanyProfileService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<CompanyProfile>> GetAllAsync()
    {
        return await _db.Companies
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<CompanyProfile?> GetByIdAsync(int id)
    {
        return await _db.Companies.FindAsync(id);
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
    {
        var query = _db.Companies.Where(c => c.Code == code);
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<(bool Success, string Error)> CreateAsync(CompanyProfile profile)
    {
        profile.Code = string.IsNullOrWhiteSpace(profile.Code) ? string.Empty : profile.Code.Trim().ToUpperInvariant();
        profile.Name = string.IsNullOrWhiteSpace(profile.Name) ? string.Empty : profile.Name.Trim();

        if (string.IsNullOrWhiteSpace(profile.Code) || string.IsNullOrWhiteSpace(profile.Name))
        {
            return (false, "Company code and name are required.");
        }

        if (await CodeExistsAsync(profile.Code))
        {
            return (false, "Company code already exists.");
        }

        _db.Companies.Add(profile);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(CompanyProfile profile)
    {
        profile.Code = string.IsNullOrWhiteSpace(profile.Code) ? string.Empty : profile.Code.Trim().ToUpperInvariant();
        profile.Name = string.IsNullOrWhiteSpace(profile.Name) ? string.Empty : profile.Name.Trim();

        var existing = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == profile.Id);
        if (existing is null)
        {
            return (false, "Company not found.");
        }

        if (await CodeExistsAsync(profile.Code, profile.Id))
        {
            return (false, "Company code already exists.");
        }

        profile.CreatedAt = existing.CreatedAt;
        profile.CreatedBy = existing.CreatedBy;

        _db.Companies.Update(profile);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var profile = await _db.Companies.FindAsync(id);
        if (profile is null)
        {
            return (false, "Company not found.");
        }

        _db.Companies.Remove(profile);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}