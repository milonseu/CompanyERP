using CompanyERP.Data;
using CompanyERP.Entities.Inventory;
using CompanyERP.Entities.Purchase;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Purchase;

public class PurchaseReturnService : IPurchaseReturnService
{
    private readonly ApplicationDbContext _db;
    private readonly IInventoryService _inventoryService;

    public PurchaseReturnService(ApplicationDbContext db, IInventoryService inventoryService)
    {
        _db = db;
        _inventoryService = inventoryService;
    }

    public async Task<List<PurchaseReturn>> GetAllAsync()
    {
        return await _db.PurchaseReturns
            .Include(r => r.Company)
            .Include(r => r.Supplier)
            .Include(r => r.Branch)
            .Include(r => r.Warehouse)
            .Include(r => r.PurchaseInvoice)
            .Include(r => r.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(r => r.ReturnDate)
            .ThenByDescending(r => r.Id)
            .ToListAsync();
    }

    public async Task<PurchaseReturn?> GetByIdAsync(int id)
    {
        return await _db.PurchaseReturns
            .Include(r => r.Company)
            .Include(r => r.Supplier)
            .Include(r => r.Branch)
            .Include(r => r.Warehouse)
            .Include(r => r.PurchaseInvoice)
            .Include(r => r.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<decimal> GetReturnedQuantityAsync(int invoiceId, int productId)
    {
        var returnedLines = await _db.PurchaseReturnLines
            .Include(l => l.PurchaseReturn)
            .Where(l => l.PurchaseReturn != null
                && l.PurchaseReturn.PurchaseInvoiceId == invoiceId
                && l.ProductId == productId)
            .ToListAsync();
        return returnedLines.Sum(l => l.Quantity);
    }

    public async Task<(bool Success, string Error)> CreateAsync(PurchaseReturn purchaseReturn, List<PurchaseReturnLine> lines)
    {
        purchaseReturn.ReturnNo = string.IsNullOrWhiteSpace(purchaseReturn.ReturnNo) ? string.Empty : purchaseReturn.ReturnNo.Trim();
        purchaseReturn.ReferenceNo = string.IsNullOrWhiteSpace(purchaseReturn.ReferenceNo) ? null : purchaseReturn.ReferenceNo.Trim();
        purchaseReturn.Note = string.IsNullOrWhiteSpace(purchaseReturn.Note) ? null : purchaseReturn.Note.Trim();

        var count = await _db.PurchaseReturns
            .CountAsync(r => r.CompanyId == purchaseReturn.CompanyId && r.ReturnDate.Date == purchaseReturn.ReturnDate.Date);
        purchaseReturn.ReturnNo = $"PRT-{purchaseReturn.ReturnDate:yyyyMMdd}-{(count + 1):D3}";

        var invoice = await _db.PurchaseInvoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == purchaseReturn.PurchaseInvoiceId);
        if (invoice is null)
        {
            return (false, "Purchase invoice not found.");
        }

        if (invoice.CompanyId != purchaseReturn.CompanyId || invoice.SupplierId != purchaseReturn.SupplierId)
        {
            return (false, "Invoice does not match the selected company and supplier.");
        }

        if (!await _db.Warehouses.AnyAsync(w => w.Id == purchaseReturn.WarehouseId && w.CompanyId == purchaseReturn.CompanyId))
        {
            return (false, "Selected warehouse does not belong to the company.");
        }

        if (!await _db.Branches.AnyAsync(b => b.Id == purchaseReturn.BranchId && b.CompanyId == purchaseReturn.CompanyId))
        {
            return (false, "Selected branch does not belong to the company.");
        }

        var active = lines.Where(l => l.ProductId != 0 && l.Quantity > 0).ToList();
        if (active.Count == 0)
        {
            return (false, "Add at least one return line with quantity.");
        }

        foreach (var line in active)
        {
            var invoiceLine = invoice.Lines.FirstOrDefault(l => l.ProductId == line.ProductId);
            if (invoiceLine is null)
            {
                return (false, "One or more products were not on the selected invoice.");
            }
            var alreadyReturned = await GetReturnedQuantityAsync(invoice.Id, line.ProductId);
            var remaining = invoiceLine.Quantity - alreadyReturned;
            if (line.Quantity > remaining)
            {
                return (false, $"Cannot return more than {remaining} of this product (already returned {alreadyReturned}).");
            }
            if (line.UnitCost < 0)
            {
                return (false, "Unit cost cannot be negative.");
            }
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            foreach (var line in active)
            {
                var result = await _inventoryService.StockOutAsync(
                    purchaseReturn.CompanyId,
                    line.ProductId,
                    purchaseReturn.WarehouseId,
                    StockTransactionType.PurchaseReturn,
                    line.Quantity,
                    line.UnitCost,
                    referenceNo: invoice.InvoiceNo,
                    transactionDate: purchaseReturn.ReturnDate,
                    note: $"Purchase return {purchaseReturn.ReturnNo}");
                if (!result.Success)
                {
                    await tx.RollbackAsync();
                    return (false, $"Stock-out failed: {result.Error}");
                }
            }

            purchaseReturn.Lines = active;
            _db.PurchaseReturns.Add(purchaseReturn);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return (true, string.Empty);
        }
        catch
        {
            await tx.RollbackAsync();
            return (false, "Purchase return failed. No changes were saved.");
        }
    }
}