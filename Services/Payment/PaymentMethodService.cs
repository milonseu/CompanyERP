using CompanyERP.Data;
using CompanyERP.Entities.Payment;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Payments;

public class PaymentMethodService : IPaymentMethodService
{
    private readonly ApplicationDbContext _db;

    public PaymentMethodService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<PaymentMethod>> GetAllAsync(int companyId)
    {
        return await _db.PaymentMethods
            .AsNoTracking()
            .Where(m => m.CompanyId == companyId)
            .Include(m => m.Company)
            .OrderBy(m => m.Code)
            .ToListAsync();
    }

    public async Task<PaymentMethod?> GetByIdAsync(int id)
    {
        return await _db.PaymentMethods
            .AsNoTracking()
            .Include(m => m.Company)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<bool> CodeExistsAsync(int companyId, string code, int? excludeId = null)
    {
        return await _db.PaymentMethods.AnyAsync(m =>
            m.CompanyId == companyId && m.Code == code && (!excludeId.HasValue || m.Id != excludeId.Value));
    }

    public async Task<(bool Success, string Error)> CreateAsync(PaymentMethod method)
    {
        method.Code = string.IsNullOrWhiteSpace(method.Code) ? string.Empty : method.Code.Trim();
        method.Name = string.IsNullOrWhiteSpace(method.Name) ? string.Empty : method.Name.Trim();
        method.Description = string.IsNullOrWhiteSpace(method.Description) ? null : method.Description.Trim();

        if (string.IsNullOrWhiteSpace(method.Code) || string.IsNullOrWhiteSpace(method.Name))
        {
            return (false, "Code and name are required.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == method.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (await CodeExistsAsync(method.CompanyId, method.Code))
        {
            return (false, $"Payment method code '{method.Code}' already exists for the company.");
        }

        _db.PaymentMethods.Add(method);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(PaymentMethod method)
    {
        var existing = await _db.PaymentMethods.AsNoTracking().FirstOrDefaultAsync(m => m.Id == method.Id);
        if (existing is null)
        {
            return (false, "Payment method not found.");
        }

        method.Code = string.IsNullOrWhiteSpace(method.Code) ? string.Empty : method.Code.Trim();
        method.Name = string.IsNullOrWhiteSpace(method.Name) ? string.Empty : method.Name.Trim();
        method.Description = string.IsNullOrWhiteSpace(method.Description) ? null : method.Description.Trim();

        if (string.IsNullOrWhiteSpace(method.Code) || string.IsNullOrWhiteSpace(method.Name))
        {
            return (false, "Code and name are required.");
        }

        if (await CodeExistsAsync(existing.CompanyId, method.Code, method.Id))
        {
            return (false, $"Payment method code '{method.Code}' already exists for the company.");
        }

        method.CompanyId = existing.CompanyId;
        method.CreatedAt = existing.CreatedAt;
        method.CreatedBy = existing.CreatedBy;
        _db.PaymentMethods.Update(method);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var method = await _db.PaymentMethods.FindAsync(id);
        if (method is null)
        {
            return (false, "Payment method not found.");
        }

        if (await _db.Payments.AnyAsync(p => p.PaymentMethodId == id))
        {
            return (false, "Payment method is used by payments and cannot be deleted.");
        }

        _db.PaymentMethods.Remove(method);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}