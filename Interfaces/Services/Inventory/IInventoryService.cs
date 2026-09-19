using CompanyERP.Entities.Inventory;

namespace CompanyERP.Interfaces.Services;

public interface IInventoryService
{
    Task<StockBalance?> GetBalanceAsync(int productId, int warehouseId);
    Task<List<StockBalance>> GetBalancesAsync(int? companyId = null, int? productId = null, int? warehouseId = null, int? branchId = null);
    Task<List<StockTransaction>> GetTransactionsAsync(int? companyId = null, int? productId = null, int? warehouseId = null, int? branchId = null);
    Task<List<StockTransfer>> GetTransfersAsync(int? companyId = null);
    Task<(bool Success, string Error)> StockInAsync(int companyId, int productId, int warehouseId, StockTransactionType type, decimal quantity, decimal unitCost, string referenceNo, DateTime transactionDate, string? note, string? toWarehouseId = null);
    Task<(bool Success, string Error)> StockOutAsync(int companyId, int productId, int warehouseId, StockTransactionType type, decimal quantity, decimal unitCost, string referenceNo, DateTime transactionDate, string? note);
    Task<(bool Success, string Error)> TransferAsync(int companyId, int productId, int fromWarehouseId, int toWarehouseId, decimal quantity, DateTime transferDate, string? note);
    Task<(bool Success, string Error)> AdjustAsync(int companyId, int productId, int warehouseId, decimal quantity, string referenceNo, DateTime transactionDate, string? note);
}