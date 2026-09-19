using CompanyERP.Data;
using CompanyERP.Entities.Employee;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Employee;

public class DesignationService : IDesignationService
{
    private readonly ApplicationDbContext _db;

    public DesignationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Designation>> GetAllAsync()
    {
        return await _db.Designations
            .Include(d => d.Company)
            .OrderBy(d => d.Company != null ? d.Company.Name : string.Empty)
            .ThenBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<List<Designation>> GetByCompanyIdAsync(int companyId)
    {
        return await _db.Designations
            .Where(d => d.CompanyId == companyId)
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<Designation?> GetByIdAsync(int id)
    {
        return await _db.Designations
            .Include(d => d.Company)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId = null)
    {
        var query = _db.Designations.Where(d => d.Code == code && d.CompanyId == companyId);
        if (excludeId.HasValue)
        {
            query = query.Where(d => d.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<bool> HasEmployeesAsync(int id)
    {
        return await _db.Employees.AnyAsync(e => e.DesignationId == id);
    }

    public async Task<(bool Success, string Error)> CreateAsync(Designation designation)
    {
        designation.Code = string.IsNullOrWhiteSpace(designation.Code) ? string.Empty : designation.Code.Trim().ToUpperInvariant();
        designation.Name = string.IsNullOrWhiteSpace(designation.Name) ? string.Empty : designation.Name.Trim();

        if (string.IsNullOrWhiteSpace(designation.Code) || string.IsNullOrWhiteSpace(designation.Name))
        {
            return (false, "Designation code and name are required.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == designation.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (await CodeExistsAsync(designation.Code, designation.CompanyId))
        {
            return (false, $"Designation code already exists for company {designation.CompanyId}.");
        }

        _db.Designations.Add(designation);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(Designation designation)
    {
        designation.Code = string.IsNullOrWhiteSpace(designation.Code) ? string.Empty : designation.Code.Trim().ToUpperInvariant();
        designation.Name = string.IsNullOrWhiteSpace(designation.Name) ? string.Empty : designation.Name.Trim();

        var existing = await _db.Designations.AsNoTracking().FirstOrDefaultAsync(d => d.Id == designation.Id);
        if (existing is null)
        {
            return (false, "Designation not found.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == designation.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (await CodeExistsAsync(designation.Code, designation.CompanyId, designation.Id))
        {
            return (false, $"Designation code already exists for company {designation.CompanyId}.");
        }

        designation.CreatedAt = existing.CreatedAt;
        designation.CreatedBy = existing.CreatedBy;

        _db.Designations.Update(designation);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var designation = await _db.Designations.FindAsync(id);
        if (designation is null)
        {
            return (false, "Designation not found.");
        }

        if (await HasEmployeesAsync(id))
        {
            return (false, "Designation cannot be deleted because it has employees.");
        }

        _db.Designations.Remove(designation);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}