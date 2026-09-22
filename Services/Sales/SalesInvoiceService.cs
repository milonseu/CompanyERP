using CompanyERP.Data;
using CompanyERP.Entities.Inventory;
using CompanyERP.Entities.Sales;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Sales;

public class SalesInvoiceService : ISalesInvoiceService
{
    private readonly ApplicationDbContext _db;
    private readonly IInventoryService _inventoryService;

    public SalesInvoiceService(ApplicationDbContext db, IInventoryService inventoryService)
    {
        _db = db;
        _inventoryService = inventoryService;
    }

    public async Task<List<SalesInvoice>> GetAllAsync()
    {
        return await _db.SalesInvoices
            .Include(i => i.Company)
            .Include(i => i.Branch)
            .Include(i => i.Customer)
            .Include(i => i.SalesOrder)
            .Include(i => i.Warehouse)
            .Include(i => i.Lines).ThenInclude(l => l.Product)
            .Include(i => i.Lines).ThenInclude(l => l.Service)
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .ToListAsync();
    }

    public async Task<SalesInvoice?> GetByIdAsync(int id)
    {
        return await _db.SalesInvoices
            .Include(i => i.Company)
            .Include(i => i.Branch)
            .Include(i => i.Customer)
            .Include(i => i.SalesOrder)
            .Include(i => i.Warehouse)
            .Include(i => i.Lines).ThenInclude(l => l.Product)
            .Include(i => i.Lines).ThenInclude(l => l.Service)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<List<SalesInvoice>> GetByCustomerIdAsync(int companyId, int customerId)
    {
        return await _db.SalesInvoices
            .Where(i => i.CompanyId == companyId && i.CustomerId == customerId)
            .Include(i => i.SalesOrder)
            .Include(i => i.Lines).ThenInclude(l => l.Product)
            .Include(i => i.Lines).ThenInclude(l => l.Service)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public async Task<(bool Success, string Error)> CreateFromOrderAsync(int orderId, DateTime invoiceDate, DateTime? dueDate, int warehouseId, SalesPaymentType paymentType, decimal amountPaid, string? note)
    {
        var order = await _db.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
        {
            return (false, "Sales order not found.");
        }

        if (order.Status != SalesOrderStatus.Confirmed)
        {
            return (false, "Only confirmed sales orders can be invoiced.");
        }

        if (order.Lines.Count == 0)
        {
            return (false, "Sales order has no lines to invoice.");
        }

        if (!await _db.Warehouses.AnyAsync(w => w.Id == warehouseId && w.CompanyId == order.CompanyId))
        {
            return (false, "Selected warehouse does not belong to the company.");
        }

        amountPaid = Math.Max(0, amountPaid);
        var orderTotal = order.Lines.Sum(l => l.Quantity * l.UnitPrice);
        if (amountPaid > orderTotal)
        {
            return (false, "Amount paid cannot exceed the invoice total.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            var count = await _db.SalesInvoices
                .CountAsync(i => i.CompanyId == order.CompanyId && i.InvoiceDate.Date == invoiceDate.Date);
            var invoiceNo = $"SI-{invoiceDate:yyyyMMdd}-{(count + 1):D3}";

            var invoice = new SalesInvoice
            {
                CompanyId = order.CompanyId,
                BranchId = order.BranchId,
                CustomerId = order.CustomerId,
                SalesOrderId = order.Id,
                WarehouseId = warehouseId,
                InvoiceNo = invoiceNo,
                InvoiceDate = invoiceDate,
                DueDate = dueDate,
                PaymentType = paymentType,
                AmountPaid = amountPaid,
                Status = amountPaid == 0
                    ? SalesInvoiceStatus.Unpaid
                    : (amountPaid >= orderTotal ? SalesInvoiceStatus.Paid : SalesInvoiceStatus.PartiallyPaid),
                Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                Lines = order.Lines.Select(l => new SalesInvoiceLine
                {
                    ItemType = l.ItemType,
                    ProductId = l.ItemType == SalesItemType.Product ? l.ProductId : null,
                    ServiceId = l.ItemType == SalesItemType.Product ? null : l.ServiceId,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice
                }).ToList()
            };

            var productIds = invoice.Lines.Where(l => l.ItemType == SalesItemType.Product && l.ProductId.HasValue)
                .Select(l => l.ProductId!.Value)
                .Distinct()
                .ToList();

            var products = await _db.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p);

            foreach (var line in invoice.Lines.Where(l => l.ItemType == SalesItemType.Product && l.ProductId.HasValue))
            {
                var product = products[line.ProductId!.Value];
                var result = await _inventoryService.StockOutAsync(
                    order.CompanyId,
                    line.ProductId!.Value,
                    warehouseId,
                    StockTransactionType.SalesStockOut,
                    line.Quantity,
                    product.CostPrice,
                    referenceNo: order.OrderNo,
                    transactionDate: invoiceDate,
                    note: $"Invoiced on {invoiceNo}");
                if (!result.Success)
                {
                    await tx.RollbackAsync();
                    return (false, result.Error);
                }
            }

            _db.SalesInvoices.Add(invoice);
            order.Status = SalesOrderStatus.Invoiced;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return (true, string.Empty);
        }
        catch
        {
            await tx.RollbackAsync();
            return (false, "Invoicing failed. No changes were saved.");
        }
    }

    public async Task<(bool Success, string Error)> RecordPaymentAsync(int invoiceId, decimal amountPaid)
    {
        var invoice = await _db.SalesInvoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoice is null)
        {
            return (false, "Sales invoice not found.");
        }

        if (amountPaid < 0)
        {
            return (false, "Amount paid cannot be negative.");
        }

        var total = invoice.Lines.Sum(l => l.Quantity * l.UnitPrice);
        if (amountPaid > total)
        {
            return (false, "Amount paid cannot exceed the invoice total.");
        }

        invoice.AmountPaid = amountPaid;
        invoice.Status = amountPaid == 0
            ? SalesInvoiceStatus.Unpaid
            : (amountPaid >= total ? SalesInvoiceStatus.Paid : SalesInvoiceStatus.PartiallyPaid);

        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}