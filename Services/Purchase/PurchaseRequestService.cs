using CompanyERP.Data;
using CompanyERP.Entities.Purchase;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Purchase;

public class PurchaseRequestService : IPurchaseRequestService
{
    private readonly ApplicationDbContext _db;

    public PurchaseRequestService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<PurchaseRequest>> GetAllAsync()
    {
        return await _db.PurchaseRequests
            .Include(r => r.Company)
            .Include(r => r.Branch)
            .Include(r => r.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(r => r.RequestDate)
            .ThenByDescending(r => r.Id)
            .ToListAsync();
    }

    public async Task<PurchaseRequest?> GetByIdAsync(int id)
    {
        return await _db.PurchaseRequests
            .Include(r => r.Company)
            .Include(r => r.Branch)
            .Include(r => r.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<bool> NumberExistsAsync(string requestNo, int companyId, int? excludeId = null)
    {
        var query = _db.PurchaseRequests.Where(r => r.RequestNo == requestNo && r.CompanyId == companyId);
        if (excludeId.HasValue)
        {
            query = query.Where(r => r.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<string> GenerateNumberAsync(int companyId, DateTime requestDate)
    {
        var count = await _db.PurchaseRequests
            .CountAsync(r => r.CompanyId == companyId && r.RequestDate.Date == requestDate.Date);
        return $"PRQ-{requestDate:yyyyMMdd}-{(count + 1):D3}";
    }

    public async Task<(bool Success, string Error)> CreateAsync(PurchaseRequest request, List<PurchaseRequestLine> lines)
    {
        request.RequestNo = string.IsNullOrWhiteSpace(request.RequestNo) ? string.Empty : request.RequestNo.Trim();
        request.RequestedBy = string.IsNullOrWhiteSpace(request.RequestedBy) ? null : request.RequestedBy.Trim();
        request.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        if (string.IsNullOrWhiteSpace(request.RequestNo))
        {
            return (false, "Request number is required.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == request.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (await NumberExistsAsync(request.RequestNo, request.CompanyId))
        {
            return (false, $"Request number {request.RequestNo} already exists.");
        }

        var valid = await ValidateLinesAsync(request.CompanyId, lines);
        if (!string.IsNullOrEmpty(valid))
        {
            return (false, valid);
        }

        request.Lines = lines.Where(l => l.ProductId != 0 && l.Quantity > 0).ToList();
        _db.PurchaseRequests.Add(request);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(PurchaseRequest request, List<PurchaseRequestLine> lines)
    {
        request.RequestNo = string.IsNullOrWhiteSpace(request.RequestNo) ? string.Empty : request.RequestNo.Trim();

        var existing = await _db.PurchaseRequests
            .Include(r => r.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id);
        if (existing is null)
        {
            return (false, "Purchase request not found.");
        }

        if (existing.Status != PurchaseRequestStatus.Draft)
        {
            return (false, "Only draft requests can be edited.");
        }

        if (await NumberExistsAsync(request.RequestNo, request.CompanyId, request.Id))
        {
            return (false, $"Request number {request.RequestNo} already exists.");
        }

        var valid = await ValidateLinesAsync(request.CompanyId, lines);
        if (!string.IsNullOrEmpty(valid))
        {
            return (false, valid);
        }

        var retained = lines.Where(l => l.ProductId != 0 && l.Quantity > 0)
            .Select(l => new PurchaseRequestLine
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                PurchaseRequestId = request.Id
            })
            .ToList();

        request.CreatedAt = existing.CreatedAt;
        request.CreatedBy = existing.CreatedBy;
        request.Lines = retained;

        _db.PurchaseRequestLines.RemoveRange(existing.Lines);
        _db.PurchaseRequests.Update(request);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateStatusAsync(int id, PurchaseRequestStatus status)
    {
        var request = await _db.PurchaseRequests.FindAsync(id);
        if (request is null)
        {
            return (false, "Purchase request not found.");
        }

        if (status == PurchaseRequestStatus.Cancelled && request.Status != PurchaseRequestStatus.Draft)
        {
            return (false, "Only draft requests can be cancelled.");
        }

        if (status == PurchaseRequestStatus.Approved)
        {
            if (request.Status != PurchaseRequestStatus.Draft)
            {
                return (false, "Only draft requests can be approved.");
            }
            if (!await _db.PurchaseRequestLines.AnyAsync(l => l.PurchaseRequestId == id))
            {
                return (false, "Cannot approve a request without lines.");
            }
        }

        if (request.Status == PurchaseRequestStatus.Converted || request.Status == PurchaseRequestStatus.Cancelled)
        {
            return (false, "This request cannot change status.");
        }

        request.Status = status;
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var request = await _db.PurchaseRequests.FindAsync(id);
        if (request is null)
        {
            return (false, "Purchase request not found.");
        }

        if (request.Status != PurchaseRequestStatus.Draft)
        {
            return (false, "Only draft requests can be deleted.");
        }

        _db.PurchaseRequestLines.RemoveRange(_db.PurchaseRequestLines.Where(l => l.PurchaseRequestId == id));
        _db.PurchaseRequests.Remove(request);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    private async Task<string> ValidateLinesAsync(int companyId, List<PurchaseRequestLine> lines)
    {
        var active = lines.Where(l => l.ProductId != 0 && l.Quantity > 0).ToList();
        if (active.Count == 0)
        {
            return "Add at least one product line with quantity.";
        }
        if (active.Any(l => l.ProductId == 0))
        {
            return "Product is required on every line.";
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