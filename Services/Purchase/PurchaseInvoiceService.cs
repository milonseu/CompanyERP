using CompanyERP.Data;
using CompanyERP.Entities.Purchase;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Purchase;

public class PurchaseInvoiceService : IPurchaseInvoiceService
{
    private readonly ApplicationDbContext _db;
    private readonly ITransactionPostingService _postingService;

    public PurchaseInvoiceService(ApplicationDbContext db, ITransactionPostingService postingService)
    {
        _db = db;
        _postingService = postingService;
    }

    public async Task<List<PurchaseInvoice>> GetAllAsync()
    {
        return await _db.PurchaseInvoices
            .Include(i => i.Company)
            .Include(i => i.Branch)
            .Include(i => i.Supplier)
            .Include(i => i.PurchaseOrder)
            .Include(i => i.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .ToListAsync();
    }

    public async Task<PurchaseInvoice?> GetByIdAsync(int id)
    {
        return await _db.PurchaseInvoices
            .Include(i => i.Company)
            .Include(i => i.Branch)
            .Include(i => i.Supplier)
            .Include(i => i.PurchaseOrder).ThenInclude(o => o != null ? o.Warehouse : null)
            .Include(i => i.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<List<PurchaseInvoice>> GetBySupplierIdAsync(int companyId, int supplierId)
    {
        return await _db.PurchaseInvoices
            .Where(i => i.CompanyId == companyId && i.SupplierId == supplierId)
            .Include(i => i.PurchaseOrder)
            .Include(i => i.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public async Task<(bool Success, string Error)> CreateFromOrderAsync(PurchaseOrder order, DateTime invoiceDate, DateTime? dueDate, string note)
    {
        var count = await _db.PurchaseInvoices
            .CountAsync(i => i.CompanyId == order.CompanyId && i.InvoiceDate.Date == invoiceDate.Date);
        var invoiceNo = $"PI-{invoiceDate:yyyyMMdd}-{(count + 1):D3}";

        var invoice = new PurchaseInvoice
        {
            CompanyId = order.CompanyId,
            BranchId = order.BranchId,
            SupplierId = order.SupplierId,
            PurchaseOrderId = order.Id,
            InvoiceNo = invoiceNo,
            InvoiceDate = invoiceDate,
            DueDate = dueDate,
            Status = PurchaseInvoiceStatus.Unpaid,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            Lines = order.Lines.Select(l => new PurchaseInvoiceLine
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitCost
            }).ToList()
        };

            // If the goods have already been received at a different cost, that cost is what inventory
            // is carried at; the invoice price still drives the payable and the gap is a variance.
            // Grouped rather than ToDictionaryAsync because a product can be received on more than
            // one receipt against the same order.
            var receivedCosts = (await _db.PurchaseReceivings
                .Include(r => r.Lines)
                .Where(r => r.PurchaseOrderId == order.Id)
                .SelectMany(r => r.Lines)
                .Where(l => l.Quantity > 0)
                .ToListAsync())
                .GroupBy(l => l.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(l => l.Quantity * l.UnitCost) / g.Sum(l => l.Quantity));

        // NOTE: Accounting effect is created during Accounting module integration:
        // Debit Inventory, Credit Accounts Payable.
        var post = await _postingService.PostPurchaseInvoiceAsync(invoice, receivedCosts);
        if (!post.Success)
        {
            return (false, post.Error);
        }

        _db.PurchaseInvoices.Add(invoice);
        await _db.SaveChangesAsync();
        return (true, invoiceNo);
    }
}