using CompanyERP.Data;
using CompanyERP.Entities.Company;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Company;

public class AccountingPeriodService : IAccountingPeriodService
{
    private readonly ApplicationDbContext _db;

    public AccountingPeriodService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<AccountingPeriod>> GetAllAsync(int? financialYearId = null)
    {
        var query = _db.AccountingPeriods
            .Include(p => p.FinancialYear)
            .AsQueryable();

        if (financialYearId.HasValue)
        {
            query = query.Where(p => p.FinancialYearId == financialYearId.Value);
        }

        return await query
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();
    }

    public async Task<AccountingPeriod?> GetByIdAsync(int id)
    {
        return await _db.AccountingPeriods
            .Include(p => p.FinancialYear)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<FinancialYear>> GetFinancialYearsAsync()
    {
        return await _db.FinancialYears
            .OrderByDescending(f => f.StartDate)
            .ToListAsync();
    }

    public async Task<FinancialYear?> GetFinancialYearAsync(int id)
    {
        return await _db.FinancialYears.FindAsync(id);
    }

    public async Task<bool> PeriodCodeExistsAsync(int financialYearId, string periodCode, int? excludeId = null)
    {
        var query = _db.AccountingPeriods
            .Where(p => p.FinancialYearId == financialYearId && p.PeriodCode == periodCode);
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<bool> HasOverlapAsync(int financialYearId, DateTime startDate, DateTime endDate, int? excludeId = null)
    {
        var query = _db.AccountingPeriods
            .Where(p => p.FinancialYearId == financialYearId &&
                        p.StartDate <= endDate && p.EndDate >= startDate);
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<(bool Success, string Error)> CreateAsync(AccountingPeriod period)
    {
        period.PeriodCode = string.IsNullOrWhiteSpace(period.PeriodCode) ? string.Empty : period.PeriodCode.Trim().ToUpperInvariant();
        period.Name = string.IsNullOrWhiteSpace(period.Name) ? string.Empty : period.Name.Trim();

        var financialYear = await GetFinancialYearAsync(period.FinancialYearId);
        if (financialYear is null)
        {
            return (false, "Selected financial year does not exist.");
        }

        if (financialYear.IsClosed)
        {
            return (false, "Cannot add a period to a closed financial year.");
        }

        if (period.StartDate >= period.EndDate)
        {
            return (false, "End date must be after start date.");
        }

        if (period.StartDate < financialYear.StartDate || period.EndDate > financialYear.EndDate)
        {
            return (false, "Period must be inside the financial year date range.");
        }

        if (await PeriodCodeExistsAsync(period.FinancialYearId, period.PeriodCode))
        {
            return (false, "Period code already exists for this financial year.");
        }

        if (await HasOverlapAsync(period.FinancialYearId, period.StartDate, period.EndDate))
        {
            return (false, "Period overlaps with an existing period of this financial year.");
        }

        _db.AccountingPeriods.Add(period);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(AccountingPeriod period)
    {
        period.PeriodCode = string.IsNullOrWhiteSpace(period.PeriodCode) ? string.Empty : period.PeriodCode.Trim().ToUpperInvariant();
        period.Name = string.IsNullOrWhiteSpace(period.Name) ? string.Empty : period.Name.Trim();

        var existing = await _db.AccountingPeriods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == period.Id);
        if (existing is null)
        {
            return (false, "Accounting period not found.");
        }

        var financialYear = await GetFinancialYearAsync(period.FinancialYearId);
        if (financialYear is null)
        {
            return (false, "Selected financial year does not exist.");
        }

        if (financialYear.IsClosed)
        {
            return (false, "Cannot update a period of a closed financial year.");
        }

        if (period.StartDate >= period.EndDate)
        {
            return (false, "End date must be after start date.");
        }

        if (period.StartDate < financialYear.StartDate || period.EndDate > financialYear.EndDate)
        {
            return (false, "Period must be inside the financial year date range.");
        }

        if (await PeriodCodeExistsAsync(period.FinancialYearId, period.PeriodCode, period.Id))
        {
            return (false, "Period code already exists for this financial year.");
        }

        if (await HasOverlapAsync(period.FinancialYearId, period.StartDate, period.EndDate, period.Id))
        {
            return (false, "Period overlaps with an existing period of this financial year.");
        }

        period.CreatedAt = existing.CreatedAt;
        period.CreatedBy = existing.CreatedBy;

        _db.AccountingPeriods.Update(period);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var period = await _db.AccountingPeriods.FindAsync(id);
        if (period is null)
        {
            return (false, "Accounting period not found.");
        }

        _db.AccountingPeriods.Remove(period);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}