using CompanyERP.Data;
using CompanyERP.Entities.Sales;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Sales;

public class ServiceDeliveryService : IServiceDeliveryService
{
    private readonly ApplicationDbContext _db;

    public ServiceDeliveryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<ServiceDelivery>> GetAllAsync()
    {
        return await _db.ServiceDeliveries
            .Include(d => d.Company)
            .Include(d => d.Branch)
            .Include(d => d.ServiceOrder)!.ThenInclude(o => o!.Customer)
            .Include(d => d.ServiceOrder)!.ThenInclude(o => o!.Service)
            .OrderByDescending(d => d.DeliveryDate)
            .ThenByDescending(d => d.Id)
            .ToListAsync();
    }

    public async Task<ServiceDelivery?> GetByIdAsync(int id)
    {
        return await _db.ServiceDeliveries
            .Include(d => d.Company)
            .Include(d => d.Branch)
            .Include(d => d.ServiceOrder)!.ThenInclude(o => o!.Customer)
            .Include(d => d.ServiceOrder)!.ThenInclude(o => o!.Service)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<(bool Success, string Error)> DeliverAsync(int companyId, int branchId, int serviceOrderId, DateTime deliveryDate, string? deliveredBy, decimal quantity, string? note)
    {
        if (!await _db.Companies.AnyAsync(c => c.Id == companyId))
        {
            return (false, "Company does not exist.");
        }

        if (!await _db.Branches.AnyAsync(b => b.Id == branchId && b.CompanyId == companyId))
        {
            return (false, "Selected branch does not belong to the company.");
        }

        var order = await _db.ServiceOrders
            .Include(o => o.Deliveries)
            .FirstOrDefaultAsync(o => o.Id == serviceOrderId);
        if (order is null || order.CompanyId != companyId)
        {
            return (false, "Selected service order does not belong to the company.");
        }

        if (order.Status is ServiceOrderStatus.Cancelled or ServiceOrderStatus.Delivered)
        {
            return (false, "Only pending or in-progress service orders can be delivered.");
        }

        if (quantity <= 0)
        {
            return (false, "Delivery quantity must be positive.");
        }

        var deliveredSoFar = order.Deliveries.Sum(d => d.Quantity);
        if (quantity > order.Quantity - deliveredSoFar)
        {
            return (false, $"Cannot deliver more than the ordered quantity. Remaining: {order.Quantity - deliveredSoFar}");
        }

        deliveredBy = string.IsNullOrWhiteSpace(deliveredBy) ? null : deliveredBy.Trim();
        note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        var delivery = new ServiceDelivery
        {
            CompanyId = companyId,
            BranchId = branchId,
            ServiceOrderId = order.Id,
            DeliveryDate = deliveryDate,
            DeliveredBy = deliveredBy,
            Quantity = quantity,
            Note = note
        };

        _db.ServiceDeliveries.Add(delivery);

        var cumulative = deliveredSoFar + quantity;
        order.Status = cumulative >= order.Quantity ? ServiceOrderStatus.Delivered : ServiceOrderStatus.InProgress;

        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}