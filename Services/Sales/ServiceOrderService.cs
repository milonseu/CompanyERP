using CompanyERP.Data;
using CompanyERP.Entities.Sales;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Sales;

public class ServiceOrderService : IServiceOrderService
{
    private readonly ApplicationDbContext _db;

    public ServiceOrderService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<ServiceOrder>> GetAllAsync()
    {
        return await _db.ServiceOrders
            .Include(o => o.Company)
            .Include(o => o.Branch)
            .Include(o => o.Customer)
            .Include(o => o.Service)
            .Include(o => o.Deliveries)
            .OrderByDescending(o => o.OrderDate)
            .ThenByDescending(o => o.Id)
            .ToListAsync();
    }

    public async Task<ServiceOrder?> GetByIdAsync(int id)
    {
        return await _db.ServiceOrders
            .Include(o => o.Company)
            .Include(o => o.Branch)
            .Include(o => o.Customer)
            .Include(o => o.Service)
            .Include(o => o.Deliveries)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<List<ServiceOrder>> GetDeliveryEligibleAsync()
    {
        return await _db.ServiceOrders
            .Where(o => o.Status == ServiceOrderStatus.Pending || o.Status == ServiceOrderStatus.InProgress)
            .Include(o => o.Company)
            .Include(o => o.Branch)
            .Include(o => o.Customer)
            .Include(o => o.Service)
            .Include(o => o.Deliveries)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<string> GenerateNumberAsync(int companyId, DateTime orderDate)
    {
        var count = await _db.ServiceOrders
            .CountAsync(o => o.CompanyId == companyId && o.OrderDate.Date == orderDate.Date);
        return $"SVO-{orderDate:yyyyMMdd}-{(count + 1):D3}";
    }

    public async Task<(bool Success, string Error)> CreateAsync(ServiceOrder order)
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

        if (!await _db.Services.AnyAsync(s => s.Id == order.ServiceId && s.CompanyId == order.CompanyId))
        {
            return (false, "Selected service does not belong to the company.");
        }

        if (await _db.ServiceOrders.AnyAsync(o => o.CompanyId == order.CompanyId && o.OrderNo == order.OrderNo))
        {
            return (false, $"Order number '{order.OrderNo}' already exists for the company.");
        }

        _db.ServiceOrders.Add(order);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateStatusAsync(int id, ServiceOrderStatus status)
    {
        var order = await _db.ServiceOrders
            .Include(o => o.Deliveries)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            return (false, "Service order not found.");
        }

        if (order.Status == ServiceOrderStatus.Delivered)
        {
            return (false, "A delivered service order cannot change status.");
        }

        if (status == ServiceOrderStatus.Cancelled && order.Status != ServiceOrderStatus.Pending)
        {
            return (false, "Only pending service orders can be cancelled.");
        }

        if (status == ServiceOrderStatus.InProgress && order.Status != ServiceOrderStatus.Pending)
        {
            return (false, "Only pending service orders can move to in progress.");
        }

        order.Status = status;
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var order = await _db.ServiceOrders.FindAsync(id);
        if (order is null)
        {
            return (false, "Service order not found.");
        }

        if (order.Status != ServiceOrderStatus.Pending)
        {
            return (false, "Only pending service orders can be deleted.");
        }

        if (await _db.ServiceDeliveries.AnyAsync(d => d.ServiceOrderId == id))
        {
            return (false, "Cannot delete an order that has deliveries.");
        }

        _db.ServiceOrders.Remove(order);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}