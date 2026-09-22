using CompanyERP.Data;
using CompanyERP.Entities.Sales;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Sales;

public class ServiceService : IServiceService
{
    private readonly ApplicationDbContext _db;

    public ServiceService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Service>> GetAllAsync()
    {
        return await _db.Services
            .Include(s => s.Company)
            .OrderBy(s => s.Code)
            .ToListAsync();
    }

    public async Task<List<Service>> GetByCompanyIdAsync(int companyId)
    {
        return await _db.Services
            .AsNoTracking()
            .Where(s => s.CompanyId == companyId)
            .OrderBy(s => s.Code)
            .ToListAsync();
    }

    public async Task<Service?> GetByIdAsync(int id)
    {
        return await _db.Services
            .Include(s => s.Company)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId = null)
    {
        var query = _db.Services.Where(s => s.CompanyId == companyId && s.Code == code);
        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<(bool Success, string Error)> CreateAsync(Service service)
    {
        service.Code = string.IsNullOrWhiteSpace(service.Code) ? string.Empty : service.Code.Trim().ToUpperInvariant();
        service.Name = string.IsNullOrWhiteSpace(service.Name) ? string.Empty : service.Name.Trim();
        service.Description = string.IsNullOrWhiteSpace(service.Description) ? null : service.Description.Trim();

        if (string.IsNullOrWhiteSpace(service.Code) || string.IsNullOrWhiteSpace(service.Name))
        {
            return (false, "Service code and name are required.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == service.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (service.UnitPrice < 0 || service.CostPrice < 0)
        {
            return (false, "Prices cannot be negative.");
        }

        if (await CodeExistsAsync(service.Code, service.CompanyId))
        {
            return (false, $"Service code '{service.Code}' already exists for the company.");
        }

        _db.Services.Add(service);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(Service service)
    {
        service.Code = string.IsNullOrWhiteSpace(service.Code) ? string.Empty : service.Code.Trim().ToUpperInvariant();
        service.Name = string.IsNullOrWhiteSpace(service.Name) ? string.Empty : service.Name.Trim();
        service.Description = string.IsNullOrWhiteSpace(service.Description) ? null : service.Description.Trim();

        var existing = await _db.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == service.Id);
        if (existing is null)
        {
            return (false, "Service not found.");
        }

        if (string.IsNullOrWhiteSpace(service.Code) || string.IsNullOrWhiteSpace(service.Name))
        {
            return (false, "Service code and name are required.");
        }

        if (service.UnitPrice < 0 || service.CostPrice < 0)
        {
            return (false, "Prices cannot be negative.");
        }

        if (await CodeExistsAsync(service.Code, existing.CompanyId, service.Id))
        {
            return (false, $"Service code '{service.Code}' already exists for the company.");
        }

        service.CompanyId = existing.CompanyId;
        service.CreatedAt = existing.CreatedAt;
        service.CreatedBy = existing.CreatedBy;
        _db.Services.Update(service);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var service = await _db.Services.FindAsync(id);
        if (service is null)
        {
            return (false, "Service not found.");
        }

        if (await _db.SalesOrderLines.AnyAsync(l => l.ServiceId == id)
            || await _db.SalesInvoiceLines.AnyAsync(l => l.ServiceId == id)
            || await _db.ServiceOrders.AnyAsync(o => o.ServiceId == id))
        {
            return (false, "Cannot delete a service that is used by sales orders, invoices or service orders.");
        }

        _db.Services.Remove(service);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}