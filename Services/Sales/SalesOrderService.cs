using CompanyERP.Data;
using CompanyERP.Entities.Sales;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Sales;

public class SalesOrderService : ISalesOrderService
{
    private readonly ApplicationDbContext _db;

    public SalesOrderService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<SalesOrder>> GetAllAsync()
    {
        return await _db.SalesOrders
            .Include(o => o.Company)
            .Include(o => o.Branch)
            .Include(o => o.Customer)
            .Include(o => o.Lines).ThenInclude(l => l.Product)
            .Include(o => o.Lines).ThenInclude(l => l.Service)
            .OrderByDescending(o => o.OrderDate)
            .ThenByDescending(o => o.Id)
            .ToListAsync();
    }

    public async Task<SalesOrder?> GetByIdAsync(int id)
    {
        return await _db.SalesOrders
            .Include(o => o.Company)
            .Include(o => o.Branch)
            .Include(o => o.Customer)
            .Include(o => o.Lines).ThenInclude(l => l.Product)
            .Include(o => o.Lines).ThenInclude(l => l.Service)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<List<SalesOrder>> GetConfirmedOrdersAsync()
    {
        return await _db.SalesOrders
            .Where(o => o.Status == SalesOrderStatus.Confirmed)
            .Include(o => o.Company)
            .Include(o => o.Branch)
            .Include(o => o.Customer)
            .Include(o => o.Lines).ThenInclude(l => l.Product)
            .Include(o => o.Lines).ThenInclude(l => l.Service)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<string> GenerateNumberAsync(int companyId, DateTime orderDate)
    {
        var count = await _db.SalesOrders
            .CountAsync(o => o.CompanyId == companyId && o.OrderDate.Date == orderDate.Date);
        return $"SO-{orderDate:yyyyMMdd}-{(count + 1):D3}";
    }

    public async Task<(bool Success, string Error)> CreateAsync(SalesOrder order, List<SalesOrderLine> lines)
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

        if (!await _db.Branches.AnyAsync(b => b.Id == order.BranchId && b.CompanyId == order.CompanyId))
        {
            return (false, "Selected branch does not belong to the company.");
        }

        if (!await _db.Customers.AnyAsync(c => c.Id == order.CustomerId && c.CompanyId == order.CompanyId))
        {
            return (false, "Selected customer does not belong to the company.");
        }

        if (await _db.SalesOrders.AnyAsync(o => o.CompanyId == order.CompanyId && o.OrderNo == order.OrderNo))
        {
            return (false, $"Order number '{order.OrderNo}' already exists for the company.");
        }

        var valid = await ValidateLinesAsync(order.CompanyId, lines);
        if (!string.IsNullOrEmpty(valid))
        {
            return (false, valid);
        }

        order.Lines = lines
            .Where(l => ((l.ItemType == SalesItemType.Product && l.ProductId.HasValue) || (l.ItemType != SalesItemType.Product && l.ServiceId.HasValue)) && l.Quantity > 0)
            .ToList();
        _db.SalesOrders.Add(order);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(SalesOrder order, List<SalesOrderLine> lines)
    {
        order.OrderNo = string.IsNullOrWhiteSpace(order.OrderNo) ? string.Empty : order.OrderNo.Trim();

        var existing = await _db.SalesOrders
            .Include(o => o.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == order.Id);
        if (existing is null)
        {
            return (false, "Sales order not found.");
        }

        if (existing.Status != SalesOrderStatus.Draft)
        {
            return (false, "Only draft orders can be edited.");
        }

        if (!await _db.Customers.AnyAsync(c => c.Id == order.CustomerId && c.CompanyId == order.CompanyId))
        {
            return (false, "Selected customer does not belong to the company.");
        }

        var valid = await ValidateLinesAsync(order.CompanyId, lines);
        if (!string.IsNullOrEmpty(valid))
        {
            return (false, valid);
        }

        var retained = lines
            .Where(l => ((l.ItemType == SalesItemType.Product && l.ProductId.HasValue) || (l.ItemType != SalesItemType.Product && l.ServiceId.HasValue)) && l.Quantity > 0)
            .Select(l => new SalesOrderLine
            {
                ItemType = l.ItemType,
                ProductId = l.ItemType == SalesItemType.Product ? l.ProductId : null,
                ServiceId = l.ItemType == SalesItemType.Product ? null : l.ServiceId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                SalesOrderId = order.Id
            })
            .ToList();

        order.CreatedAt = existing.CreatedAt;
        order.CreatedBy = existing.CreatedBy;
        order.Lines = retained;

        _db.SalesOrderLines.RemoveRange(existing.Lines);
        _db.SalesOrders.Update(order);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateStatusAsync(int id, SalesOrderStatus status)
    {
        var order = await _db.SalesOrders.FindAsync(id);
        if (order is null)
        {
            return (false, "Sales order not found.");
        }

        if (status == SalesOrderStatus.Confirmed)
        {
            if (order.Status != SalesOrderStatus.Draft)
            {
                return (false, "Only draft orders can be confirmed.");
            }
            if (!await _db.SalesOrderLines.AnyAsync(l => l.SalesOrderId == id))
            {
                return (false, "Cannot confirm an order without lines.");
            }
        }

        if (status == SalesOrderStatus.Cancelled && order.Status != SalesOrderStatus.Draft)
        {
            return (false, "Only draft orders can be cancelled.");
        }

        if (order.Status == SalesOrderStatus.Invoiced)
        {
            return (false, "An invoiced order cannot change status.");
        }

        order.Status = status;
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var order = await _db.SalesOrders.FindAsync(id);
        if (order is null)
        {
            return (false, "Sales order not found.");
        }

        if (order.Status != SalesOrderStatus.Draft)
        {
            return (false, "Only draft orders can be deleted.");
        }

        _db.SalesOrderLines.RemoveRange(_db.SalesOrderLines.Where(l => l.SalesOrderId == id));
        _db.SalesOrders.Remove(order);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    private async Task<string> ValidateLinesAsync(int companyId, List<SalesOrderLine> lines)
    {
        var active = lines.Where(l => l.Quantity > 0).ToList();
        if (active.Count == 0)
        {
            return "Add at least one item line with quantity.";
        }

        foreach (var line in active)
        {
            if (line.UnitPrice < 0)
            {
                return "Unit price cannot be negative.";
            }

            if (line.ItemType == SalesItemType.Product)
            {
                if (!line.ProductId.HasValue)
                {
                    return "Product line is missing a product.";
                }
                if (!await _db.Products.AnyAsync(p => p.Id == line.ProductId.Value && p.CompanyId == companyId))
                {
                    return "One or more products do not belong to the selected company.";
                }
            }
            else
            {
                if (!line.ServiceId.HasValue)
                {
                    return "Software/Service line is missing a service.";
                }
                var svc = await _db.Services.FirstOrDefaultAsync(s => s.Id == line.ServiceId.Value && s.CompanyId == companyId);
                if (svc is null)
                {
                    return "One or more services do not belong to the selected company.";
                }
                var expected = line.ItemType == SalesItemType.Software ? ServiceKind.Software : ServiceKind.Service;
                if (svc.ServiceKind != expected)
                {
                    return $"Service '{svc.Name}' is not a {line.ItemType} item.";
                }
            }
        }

        return string.Empty;
    }
}