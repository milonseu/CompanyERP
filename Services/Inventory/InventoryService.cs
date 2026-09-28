using CompanyERP.Data;
using CompanyERP.Entities.Inventory;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Inventory;

public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _db;
    private readonly ITransactionPostingService _postingService;

    public InventoryService(ApplicationDbContext db, ITransactionPostingService postingService)
    {
        _db = db;
        _postingService = postingService;
    }

    public async Task<StockBalance?> GetBalanceAsync(int productId, int warehouseId)
    {
        return await _db.StockBalances
            .Include(sb => sb.Product)
            .Include(sb => sb.Warehouse)
            .FirstOrDefaultAsync(sb => sb.ProductId == productId && sb.WarehouseId == warehouseId);
    }

    public async Task<List<StockBalance>> GetBalancesAsync(int? companyId = null, int? productId = null, int? warehouseId = null, int? branchId = null)
    {
        var query = _db.StockBalances
            .Include(sb => sb.Product)
            .Include(sb => sb.Warehouse)
            .Include(sb => sb.Warehouse!.Branch)
            .Include(sb => sb.Product!.Category)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(sb => sb.Product!.CompanyId == companyId.Value && sb.Warehouse!.CompanyId == companyId.Value);
        }
        if (productId.HasValue)
        {
            query = query.Where(sb => sb.ProductId == productId.Value);
        }
        if (warehouseId.HasValue)
        {
            query = query.Where(sb => sb.WarehouseId == warehouseId.Value);
        }
        if (branchId.HasValue)
        {
            query = query.Where(sb => sb.Warehouse!.BranchId == branchId.Value);
        }

        return await query
            .OrderBy(sb => sb.Product != null ? sb.Product.Name : string.Empty)
            .ThenBy(sb => sb.Warehouse != null ? sb.Warehouse.Name : string.Empty)
            .ToListAsync();
    }

    public async Task<List<StockTransaction>> GetTransactionsAsync(int? companyId = null, int? productId = null, int? warehouseId = null, int? branchId = null)
    {
        var query = _db.StockTransactions
            .Include(t => t.Product)
            .Include(t => t.Warehouse)
            .Include(t => t.Warehouse!.Branch)
            .Include(t => t.ToWarehouse)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(t => t.CompanyId == companyId.Value);
        }
        if (productId.HasValue)
        {
            query = query.Where(t => t.ProductId == productId.Value);
        }
        if (warehouseId.HasValue)
        {
            query = query.Where(t => t.WarehouseId == warehouseId.Value);
        }
        if (branchId.HasValue)
        {
            query = query.Where(t => t.Warehouse!.BranchId == branchId.Value);
        }

        return await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<StockTransfer>> GetTransfersAsync(int? companyId = null)
    {
        var query = _db.StockTransfers
            .Include(t => t.Product)
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(t => t.CompanyId == companyId.Value);
        }

        return await query
            .OrderByDescending(t => t.TransferDate)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<(bool Success, string Error, decimal AverageCost)> StockInAsync(
        int companyId, int productId, int warehouseId, StockTransactionType type,
        decimal quantity, decimal unitCost, string referenceNo, DateTime transactionDate, string? note, string? toWarehouseId = null)
    {
        if (!await _db.Companies.AnyAsync(c => c.Id == companyId))
        {
            return (false, "Company does not exist.", 0m);
        }

        if (!await _db.Products.AnyAsync(p => p.Id == productId && p.CompanyId == companyId))
        {
            return (false, "Product does not belong to this company.", 0m);
        }

        if (!await _db.Warehouses.AnyAsync(w => w.Id == warehouseId && w.CompanyId == companyId))
        {
            return (false, "Warehouse does not belong to this company.", 0m);
        }

        if (quantity <= 0)
        {
            return (false, "Stock-in quantity must be positive.", 0m);
        }

        if (unitCost < 0)
        {
            return (false, "Unit cost cannot be negative.", 0m);
        }

        referenceNo = string.IsNullOrWhiteSpace(referenceNo) ? string.Empty : referenceNo.Trim();
        if (string.IsNullOrWhiteSpace(referenceNo))
        {
            return (false, "Reference number is required.", 0m);
        }

        var balance = await GetBalanceOrCreateAsync(productId, warehouseId);

        // The journal and the stock subledger have to land together. TransactionPostingService may
        // save newly seeded chart of accounts mid-post, so without this a failure could leave a
        // stock movement committed with no accounting entry. Sales returns, purchase receipts and
        // the other module callers already own a transaction; opening a second one on the same
        // connection fails, so join theirs instead and only start one when nobody else has.
        var ownsTransaction = _db.Database.CurrentTransaction is null;
        var tx = ownsTransaction ? await _db.Database.BeginTransactionAsync() : null;
        try
        {
            var receiptValue = Round2(quantity * unitCost);
            balance.Quantity += quantity;
            balance.TotalValue = Round2(balance.TotalValue + receiptValue);
            balance.AverageCost = RecalculateAverage(balance);

            var txRow = new StockTransaction
            {
                CompanyId = companyId,
                ProductId = productId,
                WarehouseId = warehouseId,
                Type = type,
                Quantity = quantity,
                UnitCost = unitCost,
                ReferenceNo = referenceNo,
                TransactionDate = transactionDate,
                Note = note,
                ToWarehouseId = string.IsNullOrWhiteSpace(toWarehouseId) ? null : int.Parse(toWarehouseId)
            };

            _db.StockTransactions.Add(txRow);

            // Only movements that do not belong to a purchasing/sales document carry their own journal.
            // Purchase receipts, sales issues and sales returns are posted by the module that owns them.
            var post = await PostMovementJournalAsync(companyId, type, transactionDate, referenceNo,
                receiptValue, $"Stock in {type} for reference {referenceNo}", note);
            if (!post.Success)
            {
                if (ownsTransaction) { await tx!.RollbackAsync(); }
                return (false, post.Error, 0m);
            }

            await _db.SaveChangesAsync();
            if (ownsTransaction) { await tx!.CommitAsync(); }
            return (true, string.Empty, balance.AverageCost);
        }
        catch
        {
            if (ownsTransaction) { await tx!.RollbackAsync(); }
            return (false, "Stock-in failed. No changes were saved.", 0m);
        }
    }

    public async Task<(bool Success, string Error, decimal UnitCost)> StockOutAsync(
        int companyId, int productId, int warehouseId, StockTransactionType type,
        decimal quantity, string referenceNo, DateTime transactionDate, string? note)
    {
        if (!await _db.Companies.AnyAsync(c => c.Id == companyId))
        {
            return (false, "Company does not exist.", 0m);
        }

        if (!await _db.Products.AnyAsync(p => p.Id == productId && p.CompanyId == companyId))
        {
            return (false, "Product does not belong to this company.", 0m);
        }

        if (!await _db.Warehouses.AnyAsync(w => w.Id == warehouseId && w.CompanyId == companyId))
        {
            return (false, "Warehouse does not belong to this company.", 0m);
        }

        if (quantity <= 0)
        {
            return (false, "Stock-out quantity must be positive.", 0m);
        }

        referenceNo = string.IsNullOrWhiteSpace(referenceNo) ? string.Empty : referenceNo.Trim();
        if (string.IsNullOrWhiteSpace(referenceNo))
        {
            return (false, "Reference number is required.", 0m);
        }

        var balance = await _db.StockBalances
            .FirstOrDefaultAsync(sb => sb.ProductId == productId && sb.WarehouseId == warehouseId);

        if (balance is null || balance.Quantity <= 0)
        {
            return (false, "Insufficient stock. Available: 0", 0m);
        }

        if (quantity > balance.Quantity)
        {
            return (false, $"Insufficient stock. Available: {balance.Quantity}", 0m);
        }

        // Moving average: the issue cost is the average this warehouse currently carries, and the
        // same figure is handed back so the caller posts an identical COGS/Inventory entry.
        var issueUnitCost = balance.AverageCost;
        var relief = Round2(quantity * issueUnitCost);
        var remainingQty = Round2(balance.Quantity - quantity);
        if (remainingQty <= 0)
        {
            // The stored average is rounded for display, so emptying a balance can leave a fraction
            // of a cent behind. Clear it and relieve it together, otherwise the subledger keeps
            // value the ledger has already written off.
            relief = Round2(balance.TotalValue);
        }

        // Issue value and its journal land together, and a caller that already owns a transaction
        // (sales invoicing, purchase issues) shares it rather than opening a nested one.
        var ownsTransaction = _db.Database.CurrentTransaction is null;
        var tx = ownsTransaction ? await _db.Database.BeginTransactionAsync() : null;
        try
        {
            balance.Quantity -= quantity;
            balance.TotalValue = remainingQty <= 0 ? 0m : Math.Max(0, Round2(balance.TotalValue - relief));
            balance.AverageCost = RecalculateAverage(balance);

            var txRow = new StockTransaction
            {
                CompanyId = companyId,
                ProductId = productId,
                WarehouseId = warehouseId,
                Type = type,
                Quantity = -quantity,
                UnitCost = issueUnitCost,
                ReferenceNo = referenceNo,
                TransactionDate = transactionDate,
                Note = note
            };

            _db.StockTransactions.Add(txRow);

            var post = await PostMovementJournalAsync(companyId, type, transactionDate, referenceNo,
                relief, $"Stock out {type} for reference {referenceNo}", note);
            if (!post.Success)
            {
                if (ownsTransaction) { await tx!.RollbackAsync(); }
                return (false, post.Error, 0m);
            }

            await _db.SaveChangesAsync();
            if (ownsTransaction) { await tx!.CommitAsync(); }
            return (true, string.Empty, issueUnitCost);
        }
        catch
        {
            if (ownsTransaction) { await tx!.RollbackAsync(); }
            return (false, "Stock-out failed. No changes were saved.", 0m);
        }
    }

    public async Task<(bool Success, string Error, decimal MovedValue)> TransferAsync(
        int companyId, int productId, int fromWarehouseId, int toWarehouseId,
        decimal quantity, DateTime transferDate, string? note)
    {
        if (!await _db.Companies.AnyAsync(c => c.Id == companyId))
        {
            return (false, "Company does not exist.", 0m);
        }

        if (!await _db.Products.AnyAsync(p => p.Id == productId && p.CompanyId == companyId))
        {
            return (false, "Product does not belong to this company.", 0m);
        }

        if (!await _db.Warehouses.AnyAsync(w => w.Id == fromWarehouseId && w.CompanyId == companyId))
        {
            return (false, "From warehouse does not belong to this company.", 0m);
        }

        if (!await _db.Warehouses.AnyAsync(w => w.Id == toWarehouseId && w.CompanyId == companyId))
        {
            return (false, "To warehouse does not belong to this company.", 0m);
        }

        if (fromWarehouseId == toWarehouseId)
        {
            return (false, "From warehouse and to warehouse cannot be the same.", 0m);
        }

        if (quantity <= 0)
        {
            return (false, "Transfer quantity must be positive.", 0m);
        }

        var fromBalance = await _db.StockBalances
            .FirstOrDefaultAsync(sb => sb.ProductId == productId && sb.WarehouseId == fromWarehouseId);

        if (fromBalance is null || fromBalance.Quantity <= 0)
        {
            return (false, "Insufficient stock. Available: 0", 0m);
        }

        if (quantity > fromBalance.Quantity)
        {
            return (false, $"Insufficient stock. Available: {fromBalance.Quantity}", 0m);
        }

        // One cost for both legs, so total inventory value is identical before and after the move.
        // No journal is produced: this is an internal relocation, not a change in ownership.
        var transferUnitCost = fromBalance.AverageCost;
        var remainingQty = Round2(fromBalance.Quantity - quantity);
        // Emptying the source moves its whole carried value, rounding remainder included, so the two
        // warehouses together always hold exactly what they held before.
        var movedValue = remainingQty <= 0
            ? Round2(fromBalance.TotalValue)
            : Round2(quantity * transferUnitCost);

        // Both legs move together, so a failure cannot strand stock in the wrong warehouse. Join a
        // caller's transaction if there already is one.
        var ownsTransaction = _db.Database.CurrentTransaction is null;
        var txScope = ownsTransaction ? await _db.Database.BeginTransactionAsync() : null;
        try
        {
            fromBalance.Quantity -= quantity;
            fromBalance.TotalValue = remainingQty <= 0
                ? 0m
                : Math.Max(0, Round2(fromBalance.TotalValue - movedValue));
            fromBalance.AverageCost = RecalculateAverage(fromBalance);

            var toBalance = await GetBalanceOrCreateAsync(productId, toWarehouseId);
            toBalance.Quantity += quantity;
            toBalance.TotalValue = Round2(toBalance.TotalValue + movedValue);
            toBalance.AverageCost = RecalculateAverage(toBalance);

            var referenceNo = $"TRF-{DateTime.Now:yyyyMMddHHmmss}";
            var txOut = new StockTransaction
            {
                CompanyId = companyId,
                ProductId = productId,
                WarehouseId = fromWarehouseId,
                Type = StockTransactionType.StockTransfer,
                Quantity = -quantity,
                UnitCost = transferUnitCost,
                ReferenceNo = referenceNo,
                TransactionDate = transferDate,
                Note = note,
                ToWarehouseId = toWarehouseId
            };

            var txIn = new StockTransaction
            {
                CompanyId = companyId,
                ProductId = productId,
                WarehouseId = toWarehouseId,
                Type = StockTransactionType.StockTransfer,
                Quantity = quantity,
                UnitCost = transferUnitCost,
                ReferenceNo = referenceNo,
                TransactionDate = transferDate,
                Note = note,
                ToWarehouseId = fromWarehouseId
            };

            var transfer = new StockTransfer
            {
                CompanyId = companyId,
                ProductId = productId,
                FromWarehouseId = fromWarehouseId,
                ToWarehouseId = toWarehouseId,
                Quantity = quantity,
                TransferDate = transferDate,
                Note = note
            };

            _db.StockTransactions.Add(txOut);
            _db.StockTransactions.Add(txIn);
            _db.StockTransfers.Add(transfer);
            await _db.SaveChangesAsync();
            if (ownsTransaction) { await txScope!.CommitAsync(); }
            return (true, string.Empty, movedValue);
        }
        catch
        {
            if (ownsTransaction) { await txScope!.RollbackAsync(); }
            return (false, "Transfer failed. No changes were saved.", 0m);
        }
    }

    public async Task<(bool Success, string Error, decimal ValueEffect)> AdjustAsync(
        int companyId, int productId, int warehouseId,
        decimal quantity, string referenceNo, DateTime transactionDate, string? note)
    {
        if (quantity == 0)
        {
            return (false, "Adjustment quantity cannot be zero.", 0m);
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == companyId))
        {
            return (false, "Company does not exist.", 0m);
        }

        if (!await _db.Products.AnyAsync(p => p.Id == productId && p.CompanyId == companyId))
        {
            return (false, "Product does not belong to this company.", 0m);
        }

        if (!await _db.Warehouses.AnyAsync(w => w.Id == warehouseId && w.CompanyId == companyId))
        {
            return (false, "Warehouse does not belong to this company.", 0m);
        }

        referenceNo = string.IsNullOrWhiteSpace(referenceNo) ? string.Empty : referenceNo.Trim();
        if (string.IsNullOrWhiteSpace(referenceNo))
        {
            return (false, "Reference number is required.", 0m);
        }

        var balance = await _db.StockBalances
            .FirstOrDefaultAsync(sb => sb.ProductId == productId && sb.WarehouseId == warehouseId);

        var currentQty = balance?.Quantity ?? 0;

        if (quantity < 0 && -quantity > currentQty)
        {
            return (false, $"Insufficient stock. Available: {currentQty}", 0m);
        }

        if (quantity > 0 && (balance is null || balance.Quantity <= 0 || balance.AverageCost <= 0))
        {
            // A surplus count has no cost of its own. Without an existing basis there is nothing to
            // value the gain at, so the user must bring the stock in at a real cost instead.
            return (false, "A positive adjustment needs an existing stock cost. Use Stock In with the opening unit cost instead.", 0m);
        }

        if (balance is null)
        {
            balance = new StockBalance
            {
                ProductId = productId,
                WarehouseId = warehouseId,
                Quantity = 0,
                AverageCost = 0,
                TotalValue = 0
            };
            _db.StockBalances.Add(balance);
        }

        // Physical count differences are valued at the average the warehouse already carries.
        var unitCost = balance.AverageCost;
        var valueEffect = Round2(quantity * unitCost);

        // Count difference and its journal land together, joining a caller's transaction if present.
        var ownsTransaction = _db.Database.CurrentTransaction is null;
        var tx = ownsTransaction ? await _db.Database.BeginTransactionAsync() : null;
        try
        {
            balance.Quantity += quantity;
            balance.TotalValue = Round2(balance.Quantity) <= 0
                ? 0m
                : Math.Max(0, Round2(balance.TotalValue + valueEffect));
            balance.AverageCost = RecalculateAverage(balance);

            var txRow = new StockTransaction
            {
                CompanyId = companyId,
                ProductId = productId,
                WarehouseId = warehouseId,
                Type = StockTransactionType.Adjustment,
                Quantity = quantity,
                UnitCost = unitCost,
                ReferenceNo = referenceNo,
                TransactionDate = transactionDate,
                Note = note
            };

            _db.StockTransactions.Add(txRow);

            var post = await _postingService.PostStockAdjustmentAsync(companyId, transactionDate, referenceNo, valueEffect,
                $"Stock adjustment {referenceNo}", note);
            if (!post.Success)
            {
                if (ownsTransaction) { await tx!.RollbackAsync(); }
                return (false, post.Error, 0m);
            }

            await _db.SaveChangesAsync();
            if (ownsTransaction) { await tx!.CommitAsync(); }
            return (true, string.Empty, valueEffect);
        }
        catch
        {
            if (ownsTransaction) { await tx!.RollbackAsync(); }
            return (false, "Stock adjustment failed. No changes were saved.", 0m);
        }
    }

    /// <summary>
    /// Routes a stock movement to the journal that owns it. Purchase and sales movements are
    /// excluded because the purchasing/sales service posts the full document (including its
    /// payable/receivable side) as a single entry; opening balances and physical count
    /// differences stand on their own and are posted here.
    /// </summary>
    private async Task<(bool Success, string Error)> PostMovementJournalAsync(
        int companyId, StockTransactionType type, DateTime transactionDate, string referenceNo,
        decimal value, string description, string? note)
    {
        switch (type)
        {
            case StockTransactionType.Opening:
                return await _postingService.PostStockOpeningAsync(companyId, transactionDate, referenceNo, value, description, note);
            case StockTransactionType.Adjustment:
                return await _postingService.PostStockAdjustmentAsync(companyId, transactionDate, referenceNo, value, description, note);
            default:
                return (true, string.Empty);
        }
    }

    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// The value ledger is the anchor: AverageCost is always derived from it, so relieving stock at
    /// the average can never create or destroy value. An empty balance carries no cost.
    /// </summary>
    private static decimal RecalculateAverage(StockBalance balance)
    {
        if (balance.Quantity <= 0)
        {
            return 0m;
        }

        return Math.Round(balance.TotalValue / balance.Quantity, 2, MidpointRounding.AwayFromZero);
    }

    private async Task<StockBalance> GetBalanceOrCreateAsync(int productId, int warehouseId)
    {
        var balance = await _db.StockBalances
            .FirstOrDefaultAsync(sb => sb.ProductId == productId && sb.WarehouseId == warehouseId);

        if (balance is null)
        {
            balance = new StockBalance
            {
                ProductId = productId,
                WarehouseId = warehouseId,
                Quantity = 0,
                AverageCost = 0,
                TotalValue = 0
            };
            _db.StockBalances.Add(balance);
        }

        return balance;
    }
}
