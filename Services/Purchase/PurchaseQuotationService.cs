using CompanyERP.Data;
using CompanyERP.Entities.Purchase;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Purchase;

public class PurchaseQuotationService : IPurchaseQuotationService
{
    private readonly ApplicationDbContext _db;

    public PurchaseQuotationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<PurchaseQuotation>> GetAllAsync()
    {
        return await _db.PurchaseQuotations
            .Include(q => q.Company)
            .Include(q => q.Supplier)
            .Include(q => q.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(q => q.QuotationDate)
            .ThenByDescending(q => q.Id)
            .ToListAsync();
    }

    public async Task<PurchaseQuotation?> GetByIdAsync(int id)
    {
        return await _db.PurchaseQuotations
            .Include(q => q.Company)
            .Include(q => q.Supplier)
            .Include(q => q.PurchaseRequest)
            .Include(q => q.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<string> GenerateNumberAsync(int companyId, DateTime quotationDate)
    {
        var count = await _db.PurchaseQuotations
            .CountAsync(q => q.CompanyId == companyId && q.QuotationDate.Date == quotationDate.Date);
        return $"QT-{quotationDate:yyyyMMdd}-{(count + 1):D3}";
    }

    public async Task<(bool Success, string Error)> CreateAsync(PurchaseQuotation quotation, List<PurchaseQuotationLine> lines)
    {
        quotation.QuotationNo = string.IsNullOrWhiteSpace(quotation.QuotationNo) ? string.Empty : quotation.QuotationNo.Trim();
        quotation.Note = string.IsNullOrWhiteSpace(quotation.Note) ? null : quotation.Note.Trim();

        if (string.IsNullOrWhiteSpace(quotation.QuotationNo))
        {
            return (false, "Quotation number is required.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == quotation.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (string.IsNullOrWhiteSpace(quotation.QuotationNo))
        {
            return (false, "Quotation number is required.");
        }

        if (!await _db.Suppliers.AnyAsync(s => s.Id == quotation.SupplierId && s.CompanyId == quotation.CompanyId))
        {
            return (false, "Selected supplier does not belong to the company.");
        }

        if (quotation.PurchaseRequestId.HasValue)
        {
            var request = await _db.PurchaseRequests.FindAsync(quotation.PurchaseRequestId.Value);
            if (request is null || request.CompanyId != quotation.CompanyId)
            {
                return (false, "Selected purchase request is not valid for this company.");
            }
            if (request.Status == PurchaseRequestStatus.Cancelled)
            {
                return (false, "Cannot create a quotation from a cancelled request.");
            }
        }

        var valid = await ValidateLinesAsync(quotation.CompanyId, lines);
        if (!string.IsNullOrEmpty(valid))
        {
            return (false, valid);
        }

        quotation.Lines = lines.Where(l => l.ProductId != 0 && l.Quantity > 0).ToList();
        _db.PurchaseQuotations.Add(quotation);
        await _db.SaveChangesAsync();

        if (quotation.PurchaseRequestId.HasValue)
        {
            var request = await _db.PurchaseRequests.FindAsync(quotation.PurchaseRequestId.Value);
            if (request != null && request.Status != PurchaseRequestStatus.Converted)
            {
                request.Status = PurchaseRequestStatus.Converted;
                await _db.SaveChangesAsync();
            }
        }

        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(PurchaseQuotation quotation, List<PurchaseQuotationLine> lines)
    {
        quotation.QuotationNo = string.IsNullOrWhiteSpace(quotation.QuotationNo) ? string.Empty : quotation.QuotationNo.Trim();

        var existing = await _db.PurchaseQuotations
            .Include(q => q.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == quotation.Id);
        if (existing is null)
        {
            return (false, "Quotation not found.");
        }

        if (existing.Status != PurchaseQuotationStatus.Draft)
        {
            return (false, "Only draft quotations can be edited.");
        }

        if (!await _db.Suppliers.AnyAsync(s => s.Id == quotation.SupplierId && s.CompanyId == quotation.CompanyId))
        {
            return (false, "Selected supplier does not belong to the company.");
        }

        var valid = await ValidateLinesAsync(quotation.CompanyId, lines);
        if (!string.IsNullOrEmpty(valid))
        {
            return (false, valid);
        }

        var retained = lines.Where(l => l.ProductId != 0 && l.Quantity > 0)
            .Select(l => new PurchaseQuotationLine
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                PurchaseQuotationId = quotation.Id
            })
            .ToList();

        quotation.CreatedAt = existing.CreatedAt;
        quotation.CreatedBy = existing.CreatedBy;
        quotation.Lines = retained;

        _db.PurchaseQuotationLines.RemoveRange(existing.Lines);
        _db.PurchaseQuotations.Update(quotation);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateStatusAsync(int id, PurchaseQuotationStatus status)
    {
        var quotation = await _db.PurchaseQuotations.FindAsync(id);
        if (quotation is null)
        {
            return (false, "Quotation not found.");
        }

        if (status == PurchaseQuotationStatus.Cancelled && quotation.Status != PurchaseQuotationStatus.Draft)
        {
            return (false, "Only draft quotations can be cancelled.");
        }

        if (quotation.Status == PurchaseQuotationStatus.Accepted)
        {
            return (false, "An accepted quotation cannot change status.");
        }

        quotation.Status = status;
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var quotation = await _db.PurchaseQuotations.FindAsync(id);
        if (quotation is null)
        {
            return (false, "Quotation not found.");
        }

        if (quotation.Status != PurchaseQuotationStatus.Draft)
        {
            return (false, "Only draft quotations can be deleted.");
        }

        _db.PurchaseQuotationLines.RemoveRange(_db.PurchaseQuotationLines.Where(l => l.PurchaseQuotationId == id));
        _db.PurchaseQuotations.Remove(quotation);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    private async Task<string> ValidateLinesAsync(int companyId, List<PurchaseQuotationLine> lines)
    {
        var active = lines.Where(l => l.ProductId != 0 && l.Quantity > 0).ToList();
        if (active.Count == 0)
        {
            return "Add at least one product line with quantity.";
        }
        var productIds = active.Select(l => l.ProductId).Distinct().ToList();
        var validCount = await _db.Products.CountAsync(p => productIds.Contains(p.Id) && p.CompanyId == companyId);
        if (validCount != productIds.Count)
        {
            return "One or more products do not belong to the selected company.";
        }
        return string.Empty;
    }
}