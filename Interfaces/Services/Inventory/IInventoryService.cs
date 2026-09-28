using CompanyERP.Entities.Inventory;

namespace CompanyERP.Interfaces.Services;

public interface IInventoryService
{
    Task<StockBalance?> GetBalanceAsync(int productId, int warehouseId);
    Task<List<StockBalance>> GetBalancesAsync(int? companyId = null, int? productId = null, int? warehouseId = null, int? branchId = null);
    Task<List<StockTransaction>> GetTransactionsAsync(int? companyId = null, int? productId = null, int? warehouseId = null, int? branchId = null);
    Task<List<StockTransfer>> GetTransfersAsync(int? companyId = null);

    /// <summary>
    /// Receipts stock at a caller supplied cost (purchase receipt, opening balance) and posts the
    /// matching journal for the types that carry their own accounting meaning. Returns the
    /// resulting moving-average unit cost so the caller can book revenue/cost against it.
    /// </summary>
    Task<(bool Success, string Error, decimal AverageCost)> StockInAsync(int companyId, int productId, int warehouseId, StockTransactionType type, decimal quantity, decimal unitCost, string referenceNo, DateTime transactionDate, string? note, string? toWarehouseId = null);

    /// <summary>
    /// Issues stock at the warehouse moving-average cost. The caller supplied cost is ignored on
    /// purpose: the returned UnitCost is the figure the subledger relieved and the figure the
    /// caller must post to COGS/Inventory, so the two cannot drift apart.
    /// </summary>
    Task<(bool Success, string Error, decimal UnitCost)> StockOutAsync(int companyId, int productId, int warehouseId, StockTransactionType type, decimal quantity, string referenceNo, DateTime transactionDate, string? note);

    /// <summary>Moves stock between warehouses at a single cost. No journal: total inventory value is unchanged.</summary>
    Task<(bool Success, string Error, decimal MovedValue)> TransferAsync(int companyId, int productId, int fromWarehouseId, int toWarehouseId, decimal quantity, DateTime transferDate, string? note);

    /// <summary>
    /// Physical count difference. A loss relieves value to the inventory write-off account, a gain
    /// adds value to inventory gain. Returns the signed value effect for the caller to report.
    /// </summary>
    Task<(bool Success, string Error, decimal ValueEffect)> AdjustAsync(int companyId, int productId, int warehouseId, decimal quantity, string referenceNo, DateTime transactionDate, string? note);
}
