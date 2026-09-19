using CompanyERP.Data;
using CompanyERP.Entities.Inventory;
using CompanyERP.Entities.Purchase;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Purchase;

public class PurchaseReceivingService : IPurchaseReceivingService
{
    private readonly ApplicationDbContext _db;
    private readonly IInventoryService _inventoryService;
    private readonly IPurchaseInvoiceService _invoiceService;

    public PurchaseReceivingService(ApplicationDbContext db, IInventoryService inventoryService, IPurchaseInvoiceService invoiceService)
    {
        _db = db;
        _inventoryService = inventoryService;
        _invoiceService = invoiceService;
    }

    public async Task<List<PurchaseReceiving>> GetAllAsync()
    {
        return await _db.PurchaseReceivings
            .Include(r => r.Company)
            .Include(r => r.Supplier)
            .Include(r => r.Warehouse)
            .Include(r => r.PurchaseOrder)
            .Include(r => r.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(r => r.ReceivedDate)
            .ThenByDescending(r => r.Id)
            .ToListAsync();
    }

    public async Task<PurchaseReceiving?> GetByIdAsync(int id)
    {
        return await _db.PurchaseReceivings
            .Include(r => r.Company)
            .Include(r => r.Supplier)
            .Include(r => r.Warehouse)
            .Include(r => r.PurchaseOrder)
            .Include(r => r.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<(bool Success, string Error)> ReceiveAsync(int orderId, DateTime receivedDate, string? referenceNo, string? note)
    {
        var order = await _db.PurchaseOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
        {
            return (false, "Purchase order not found.");
        }

        if (order.Status != PurchaseOrderStatus.Approved)
        {
            return (false, "Only approved purchase orders can be received.");
        }

        if (order.Lines.Count == 0)
        {
            return (false, "Purchase order has no lines to receive.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            var count = await _db.PurchaseReceivings
                .CountAsync(r => r.CompanyId == order.CompanyId && r.ReceivedDate.Date == receivedDate.Date);
            var receivingNo = $"RCV-{receivedDate:yyyyMMdd}-{(count + 1):D3}";

            var receiving = new PurchaseReceiving
            {
                CompanyId = order.CompanyId,
                PurchaseOrderId = order.Id,
                SupplierId = order.SupplierId,
                WarehouseId = order.WarehouseId,
                ReceivingNo = receivingNo,
                ReceivedDate = receivedDate,
                ReferenceNo = string.IsNullOrWhiteSpace(referenceNo) ? null : referenceNo.Trim(),
                Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                Lines = order.Lines.Select(l => new PurchaseReceivingLine
                {
                    ProductId = l.ProductId,
                    Quantity = l.Quantity,
                    UnitCost = l.UnitCost
                }).ToList()
            };

            foreach (var line in receiving.Lines)
            {
                var result = await _inventoryService.StockInAsync(
                    order.CompanyId,
                    line.ProductId,
                    order.WarehouseId,
                    StockTransactionType.PurchaseReceiving,
                    line.Quantity,
                    line.UnitCost,
                    referenceNo: order.OrderNo,
                    transactionDate: receivedDate,
                    note: $"Received on {receivingNo}");
                if (!result.Success)
                {
                    await tx.RollbackAsync();
                    return (false, $"Stock-in failed for one or more lines: {result.Error}");
                }
            }

            var invoiceResult = await _invoiceService.CreateFromOrderAsync(order, receivedDate, null, $"Auto-created on receiving {receivingNo}");
            if (!invoiceResult.Success)
            {
                await tx.RollbackAsync();
                return (false, invoiceResult.Error);
            }

            _db.PurchaseReceivings.Add(receiving);
            order.Status = PurchaseOrderStatus.Received;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return (true, string.Empty);
        }
        catch
        {
            await tx.RollbackAsync();
            return (false, "Receiving failed. No changes were saved.");
        }
    }
}