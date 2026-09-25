using CompanyERP.Data;
using CompanyERP.Entities.Inventory;
using CompanyERP.Entities.Sales;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Sales;

public class SalesReturnService : ISalesReturnService
{
    private readonly ApplicationDbContext _db;
    private readonly IInventoryService _inventoryService;
    private readonly ITransactionPostingService _postingService;

    public SalesReturnService(ApplicationDbContext db, IInventoryService inventoryService, ITransactionPostingService postingService)
    {
        _db = db;
        _inventoryService = inventoryService;
        _postingService = postingService;
    }

    public async Task<List<SalesReturn>> GetAllAsync()
    {
        return await _db.SalesReturns
            .Include(r => r.Company)
            .Include(r => r.Branch)
            .Include(r => r.Customer)
            .Include(r => r.SalesInvoice)
            .Include(r => r.Lines).ThenInclude(l => l.Product)
            .Include(r => r.Lines).ThenInclude(l => l.Service)
            .OrderByDescending(r => r.ReturnDate)
            .ThenByDescending(r => r.Id)
            .ToListAsync();
    }

    public async Task<SalesReturn?> GetByIdAsync(int id)
    {
        return await _db.SalesReturns
            .Include(r => r.Company)
            .Include(r => r.Branch)
            .Include(r => r.Customer)
            .Include(r => r.SalesInvoice)
            .Include(r => r.Lines).ThenInclude(l => l.Product)
            .Include(r => r.Lines).ThenInclude(l => l.Service)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Dictionary<(SalesItemType ItemType, int? ProductId, int? ServiceId), decimal>> GetReturnedByInvoiceAsync(int invoiceId)
    {
        return await _db.SalesReturns
            .Where(r => r.SalesInvoiceId == invoiceId && r.Status == SalesReturnStatus.Posted)
            .SelectMany(r => r.Lines)
            .GroupBy(l => new { l.ItemType, l.ProductId, l.ServiceId })
            .Select(g => new { g.Key.ItemType, g.Key.ProductId, g.Key.ServiceId, Qty = g.Sum(l => l.Quantity) })
            .ToDictionaryAsync(g => (g.ItemType, g.ProductId, g.ServiceId), g => g.Qty);
    }

    public async Task<(bool Success, string Error)> CreateFromInvoiceAsync(int invoiceId, DateTime returnDate, List<SalesReturnLine> lines, string? note)
    {
        var invoice = await _db.SalesInvoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoice is null)
        {
            return (false, "Sales invoice not found.");
        }

        if (invoice.Lines.Count == 0)
        {
            return (false, "Sales invoice has no lines to return.");
        }

        var valid = await ValidateLinesAsync(invoice, lines);
        if (!string.IsNullOrEmpty(valid))
        {
            return (false, valid);
        }

        var active = lines.Where(l => l.Quantity > 0).ToList();

        await using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            var count = await _db.SalesReturns
                .CountAsync(r => r.CompanyId == invoice.CompanyId && r.ReturnDate.Date == returnDate.Date);
            var returnNo = $"SR-{returnDate:yyyyMMdd}-{(count + 1):D3}";

            var salesReturn = new SalesReturn
            {
                CompanyId = invoice.CompanyId,
                BranchId = invoice.BranchId,
                CustomerId = invoice.CustomerId,
                SalesInvoiceId = invoice.Id,
                ReturnNo = returnNo,
                ReturnDate = returnDate,
                Status = SalesReturnStatus.Posted,
                Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                Lines = active.Select(l => new SalesReturnLine
                {
                    ItemType = l.ItemType,
                    ProductId = l.ItemType == SalesItemType.Product ? l.ProductId : null,
                    ServiceId = l.ItemType == SalesItemType.Product ? null : l.ServiceId,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice
                }).ToList()
            };

            var productIds = invoice.Lines
                .Where(l => l.ItemType == SalesItemType.Product && l.ProductId.HasValue)
                .Select(l => l.ProductId!.Value)
                .Distinct()
                .ToList();
            var products = await _db.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p);

            foreach (var line in salesReturn.Lines.Where(l => l.ItemType == SalesItemType.Product && l.ProductId.HasValue))
            {
                var product = products[line.ProductId!.Value];
                var result = await _inventoryService.StockInAsync(
                    invoice.CompanyId,
                    line.ProductId!.Value,
                    invoice.WarehouseId,
                    StockTransactionType.SalesReturn,
                    line.Quantity,
                    product.CostPrice,
                    referenceNo: invoice.InvoiceNo,
                    transactionDate: returnDate,
                    note: $"Sales return {returnNo}");
                if (!result.Success)
                {
                    await tx.RollbackAsync();
                    return (false, result.Error);
                }
            }

            // NOTE: Accounting effect is created during Accounting module integration:
            // reverse the revenue (Debit Revenue, Credit AR) and the COGS/Inventory.
            var post = await _postingService.PostSalesReturnAsync(salesReturn, invoice, products);
            if (!post.Success)
            {
                await tx.RollbackAsync();
                return (false, post.Error);
            }

            _db.SalesReturns.Add(salesReturn);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return (true, string.Empty);
        }
        catch
        {
            await tx.RollbackAsync();
            return (false, "Sales return failed. No changes were saved.");
        }
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var salesReturn = await _db.SalesReturns.FindAsync(id);
        if (salesReturn is null)
        {
            return (false, "Sales return not found.");
        }

        return (false, "Posted sales returns are financial records and cannot be deleted. Use an adjustment entry instead.");
    }

    private async Task<string> ValidateLinesAsync(SalesInvoice invoice, List<SalesReturnLine> lines)
    {
        var active = lines.Where(l => l.Quantity > 0).ToList();
        if (active.Count == 0)
        {
            return "Add at least one item line with quantity.";
        }

        var alreadyReturned = await _db.SalesReturns
            .Where(r => r.SalesInvoiceId == invoice.Id && r.Status == SalesReturnStatus.Posted)
            .SelectMany(r => r.Lines)
            .ToListAsync();

        foreach (var line in active)
        {
            if (line.UnitPrice < 0)
            {
                return "Refund unit price cannot be negative.";
            }

            var source = invoice.Lines.FirstOrDefault(l =>
                l.ItemType == line.ItemType
                && ((line.ItemType == SalesItemType.Product && l.ProductId == line.ProductId)
                    || (line.ItemType != SalesItemType.Product && l.ServiceId == line.ServiceId)));

            if (source is null)
            {
                return "Return line does not match any line on the invoice.";
            }

            var returned = alreadyReturned
                .Where(l => l.ItemType == line.ItemType
                    && ((line.ItemType == SalesItemType.Product && l.ProductId == line.ProductId)
                        || (line.ItemType != SalesItemType.Product && l.ServiceId == line.ServiceId)))
                .Sum(l => l.Quantity);

            if (line.Quantity > source.Quantity - returned)
            {
                return $"Return quantity exceeds the sold quantity. Available to return: {source.Quantity - returned}";
            }
        }

        return string.Empty;
    }
}