using CompanyERP.Data;
using CompanyERP.Entities.Purchase;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Purchase;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly ApplicationDbContext _db;

    public PurchaseOrderService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<PurchaseOrder>> GetAllAsync()
    {
        return await _db.PurchaseOrders
            .Include(o => o.Company)
            .Include(o => o.Supplier)
            .Include(o => o.Branch)
            .Include(o => o.Warehouse)
            .Include(o => o.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(o => o.OrderDate)
            .ThenByDescending(o => o.Id)
            .ToListAsync();
    }

    public async Task<PurchaseOrder?> GetByIdAsync(int id)
    {
        return await _db.PurchaseOrders
            .Include(o => o.Company)
            .Include(o => o.Supplier)
            .Include(o => o.Branch)
            .Include(o => o.Warehouse)
            .Include(o => o.PurchaseQuotation)
            .Include(o => o.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<List<PurchaseOrder>> GetReceivableOrdersAsync()
    {
        return await _db.PurchaseOrders
            .Where(o => o.Status == PurchaseOrderStatus.Approved)
            .Include(o => o.Company)
            .Include(o => o.Supplier)
            .Include(o => o.Branch)
            .Include(o => o.Warehouse)
            .Include(o => o.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<string> GenerateNumberAsync(int companyId, DateTime orderDate)
    {
        var count = await _db.PurchaseOrders
            .CountAsync(o => o.CompanyId == companyId && o.OrderDate.Date == orderDate.Date);
        return $"PO-{orderDate:yyyyMMdd}-{(count + 1):D3}";
    }

    public async Task<(bool Success, string Error)> CreateAsync(PurchaseOrder order, List<PurchaseOrderLine> lines)
    {
        order.OrderNo = string.IsNullOrWhiteSpace(order.OrderNo) ? string.Empty : order.OrderNo.Trim();
        order.Note = string.IsNullOrWhiteSpace(order.Note) ? null : order.Note.Trim();

        if (string.IsNullOrWhiteSpace(order.OrderNo))
        {
            return (false, "Order number is required.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == order.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (!await _db.Suppliers.AnyAsync(s => s.Id == order.SupplierId && s.CompanyId == order.CompanyId))
        {
            return (false, "Selected supplier does not belong to the company.");
        }

        if (!await _db.Branches.AnyAsync(b => b.Id == order.BranchId && b.CompanyId == order.CompanyId))
        {
            return (false, "Selected branch does not belong to the company.");
        }

        if (!await _db.Warehouses.AnyAsync(w => w.Id == order.WarehouseId && w.CompanyId == order.CompanyId))
        {
            return (false, "Selected warehouse does not belong to the company.");
        }

        if (order.PurchaseQuotationId.HasValue)
        {
            var quotation = await _db.PurchaseQuotations.FindAsync(order.PurchaseQuotationId.Value);
            if (quotation is null || quotation.CompanyId != order.CompanyId || quotation.SupplierId != order.SupplierId)
            {
                return (false, "Selected quotation is not valid for the company and supplier.");
            }
        }

        var valid = await ValidateLinesAsync(order.CompanyId, lines);
        if (!string.IsNullOrEmpty(valid))
        {
            return (false, valid);
        }

        order.Lines = lines.Where(l => l.ProductId != 0 && l.Quantity > 0).ToList();
        _db.PurchaseOrders.Add(order);
        await _db.SaveChangesAsync();

        if (order.PurchaseQuotationId.HasValue)
        {
            var quotation = await _db.PurchaseQuotations.FindAsync(order.PurchaseQuotationId.Value);
            if (quotation != null)
            {
                quotation.Status = PurchaseQuotationStatus.Accepted;
                await _db.SaveChangesAsync();
            }
        }

        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(PurchaseOrder order, List<PurchaseOrderLine> lines)
    {
        order.OrderNo = string.IsNullOrWhiteSpace(order.OrderNo) ? string.Empty : order.OrderNo.Trim();

        var existing = await _db.PurchaseOrders
            .Include(o => o.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == order.Id);
        if (existing is null)
        {
            return (false, "Purchase order not found.");
        }

        if (existing.Status != PurchaseOrderStatus.Draft)
        {
            return (false, "Only draft orders can be edited.");
        }

        if (!await _db.Suppliers.AnyAsync(s => s.Id == order.SupplierId && s.CompanyId == order.CompanyId))
        {
            return (false, "Selected supplier does not belong to the company.");
        }

        var valid = await ValidateLinesAsync(order.CompanyId, lines);
        if (!string.IsNullOrEmpty(valid))
        {
            return (false, valid);
        }

        var retained = lines.Where(l => l.ProductId != 0 && l.Quantity > 0)
            .Select(l => new PurchaseOrderLine
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                UnitCost = l.UnitCost,
                PurchaseOrderId = order.Id
            })
            .ToList();

        order.CreatedAt = existing.CreatedAt;
        order.CreatedBy = existing.CreatedBy;
        order.Lines = retained;

        _db.PurchaseOrderLines.RemoveRange(existing.Lines);
        _db.PurchaseOrders.Update(order);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateStatusAsync(int id, PurchaseOrderStatus status, int? quotationId = null)
    {
        var order = await _db.PurchaseOrders.FindAsync(id);
        if (order is null)
        {
            return (false, "Purchase order not found.");
        }

        if (status == PurchaseOrderStatus.Approved)
        {
            if (order.Status != PurchaseOrderStatus.Draft)
            {
                return (false, "Only draft orders can be approved.");
            }
            if (!await _db.PurchaseOrderLines.AnyAsync(l => l.PurchaseOrderId == id))
            {
                return (false, "Cannot approve an order without lines.");
            }
        }

        if (status == PurchaseOrderStatus.Cancelled && order.Status != PurchaseOrderStatus.Draft)
        {
            return (false, "Only draft orders can be cancelled.");
        }

        if (order.Status == PurchaseOrderStatus.Received)
        {
            return (false, "A received order cannot change status.");
        }

        order.Status = status;
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var order = await _db.PurchaseOrders.FindAsync(id);
        if (order is null)
        {
            return (false, "Purchase order not found.");
        }

        if (order.Status != PurchaseOrderStatus.Draft)
        {
            return (false, "Only draft orders can be deleted.");
        }

        _db.PurchaseOrderLines.RemoveRange(_db.PurchaseOrderLines.Where(l => l.PurchaseOrderId == id));
        _db.PurchaseOrders.Remove(order);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    private async Task<string> ValidateLinesAsync(int companyId, List<PurchaseOrderLine> lines)
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