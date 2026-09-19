using CompanyERP.Data;
using CompanyERP.Entities.Company;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Company;

public class FinancialYearService : IFinancialYearService
{
    private readonly ApplicationDbContext _db;

    public FinancialYearService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<FinancialYear>> GetAllAsync()
    {
        return await _db.FinancialYears
            .OrderByDescending(f => f.StartDate)
            .ToListAsync();
    }

    public async Task<FinancialYear?> GetByIdAsync(int id)
    {
        return await _db.FinancialYears
            .Include(f => f.AccountingPeriods)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<bool> YearCodeExistsAsync(string yearCode, int? excludeId = null)
    {
        var query = _db.FinancialYears.Where(f => f.YearCode == yearCode);
        if (excludeId.HasValue)
        {
            query = query.Where(f => f.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<bool> HasOverlapAsync(DateTime startDate, DateTime endDate, int? excludeId = null)
    {
        var query = _db.FinancialYears
            .Where(f => f.StartDate <= endDate && f.EndDate >= startDate);
        if (excludeId.HasValue)
        {
            query = query.Where(f => f.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<(bool Success, string Error)> CreateAsync(FinancialYear year)
    {
        year.YearCode = string.IsNullOrWhiteSpace(year.YearCode) ? string.Empty : year.YearCode.Trim().ToUpperInvariant();
        year.Name = string.IsNullOrWhiteSpace(year.Name) ? string.Empty : year.Name.Trim();

        if (year.StartDate >= year.EndDate)
        {
            return (false, "End date must be after start date.");
        }

        if (await YearCodeExistsAsync(year.YearCode))
        {
            return (false, "Year code already exists.");
        }

        if (await HasOverlapAsync(year.StartDate, year.EndDate))
        {
            return (false, "Financial year overlaps with an existing financial year.");
        }

        _db.FinancialYears.Add(year);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(FinancialYear year)
    {
        year.YearCode = string.IsNullOrWhiteSpace(year.YearCode) ? string.Empty : year.YearCode.Trim().ToUpperInvariant();
        year.Name = string.IsNullOrWhiteSpace(year.Name) ? string.Empty : year.Name.Trim();

        var existing = await _db.FinancialYears.AsNoTracking().FirstOrDefaultAsync(f => f.Id == year.Id);
        if (existing is null)
        {
            return (false, "Financial year not found.");
        }

        if (year.StartDate >= year.EndDate)
        {
            return (false, "End date must be after start date.");
        }

        if (await YearCodeExistsAsync(year.YearCode, year.Id))
        {
            return (false, "Year code already exists.");
        }

        if (await HasOverlapAsync(year.StartDate, year.EndDate, year.Id))
        {
            return (false, "Financial year overlaps with an existing financial year.");
        }

        year.CreatedAt = existing.CreatedAt;
        year.CreatedBy = existing.CreatedBy;

        _db.FinancialYears.Update(year);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var year = await _db.FinancialYears.FindAsync(id);
        if (year is null)
        {
            return (false, "Financial year not found.");
        }

        if (await _db.AccountingPeriods.AnyAsync(p => p.FinancialYearId == id))
        {
            return (false, "Financial year cannot be deleted because it has accounting periods.");
        }

        _db.FinancialYears.Remove(year);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}