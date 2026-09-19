using CompanyERP.Data;
using CompanyERP.Entities.Employee;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Employee;

public class DepartmentService : IDepartmentService
{
    private readonly ApplicationDbContext _db;

    public DepartmentService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Department>> GetAllAsync()
    {
        return await _db.Departments
            .Include(d => d.Company)
            .OrderBy(d => d.Company != null ? d.Company.Name : string.Empty)
            .ThenBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<List<Department>> GetByCompanyIdAsync(int companyId)
    {
        return await _db.Departments
            .Where(d => d.CompanyId == companyId)
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<Department?> GetByIdAsync(int id)
    {
        return await _db.Departments
            .Include(d => d.Company)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId = null)
    {
        var query = _db.Departments.Where(d => d.Code == code && d.CompanyId == companyId);
        if (excludeId.HasValue)
        {
            query = query.Where(d => d.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<bool> HasEmployeesAsync(int id)
    {
        return await _db.Employees.AnyAsync(e => e.DepartmentId == id);
    }

    public async Task<(bool Success, string Error)> CreateAsync(Department department)
    {
        department.Code = string.IsNullOrWhiteSpace(department.Code) ? string.Empty : department.Code.Trim().ToUpperInvariant();
        department.Name = string.IsNullOrWhiteSpace(department.Name) ? string.Empty : department.Name.Trim();

        if (string.IsNullOrWhiteSpace(department.Code) || string.IsNullOrWhiteSpace(department.Name))
        {
            return (false, "Department code and name are required.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == department.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (await CodeExistsAsync(department.Code, department.CompanyId))
        {
            return (false, $"Department code already exists for company {department.CompanyId}.");
        }

        _db.Departments.Add(department);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(Department department)
    {
        department.Code = string.IsNullOrWhiteSpace(department.Code) ? string.Empty : department.Code.Trim().ToUpperInvariant();
        department.Name = string.IsNullOrWhiteSpace(department.Name) ? string.Empty : department.Name.Trim();

        var existing = await _db.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == department.Id);
        if (existing is null)
        {
            return (false, "Department not found.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == department.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (await CodeExistsAsync(department.Code, department.CompanyId, department.Id))
        {
            return (false, $"Department code already exists for company {department.CompanyId}.");
        }

        department.CreatedAt = existing.CreatedAt;
        department.CreatedBy = existing.CreatedBy;

        _db.Departments.Update(department);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var department = await _db.Departments.FindAsync(id);
        if (department is null)
        {
            return (false, "Department not found.");
        }

        if (await HasEmployeesAsync(id))
        {
            return (false, "Department cannot be deleted because it has employees.");
        }

        _db.Departments.Remove(department);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}