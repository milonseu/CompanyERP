using CompanyERP.Data;
using CompanyERP.Entities.Inventory;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Inventory;

public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _db;

    public InventoryService(ApplicationDbContext db)
    {
        _db = db;
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

    public async Task<(bool Success, string Error)> StockInAsync(
        int companyId, int productId, int warehouseId, StockTransactionType type,
        decimal quantity, decimal unitCost, string referenceNo, DateTime transactionDate, string? note, string? toWarehouseId = null)
    {
        if (!await _db.Companies.AnyAsync(c => c.Id == companyId))
        {
            return (false, "Company does not exist.");
        }

        if (!await _db.Products.AnyAsync(p => p.Id == productId && p.CompanyId == companyId))
        {
            return (false, "Product does not belong to this company.");
        }

        if (!await _db.Warehouses.AnyAsync(w => w.Id == warehouseId && w.CompanyId == companyId))
        {
            return (false, "Warehouse does not belong to this company.");
        }

        if (quantity <= 0)
        {
            return (false, "Stock-in quantity must be positive.");
        }

        if (unitCost < 0)
        {
            return (false, "Unit cost cannot be negative.");
        }

        referenceNo = string.IsNullOrWhiteSpace(referenceNo) ? string.Empty : referenceNo.Trim();
        if (string.IsNullOrWhiteSpace(referenceNo))
        {
            return (false, "Reference number is required.");
        }

        var balance = await GetBalanceOrCreateAsync(productId, warehouseId);

        if (balance.Quantity == 0 && balance.AverageCost == 0)
        {
            balance.AverageCost = unitCost;
        }
        else
        {
            var totalCost = (balance.Quantity * balance.AverageCost) + (quantity * unitCost);
            balance.AverageCost = totalCost / (balance.Quantity + quantity);
        }

        balance.Quantity += quantity;

        var tx = new StockTransaction
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

        _db.StockTransactions.Add(tx);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> StockOutAsync(
        int companyId, int productId, int warehouseId, StockTransactionType type,
        decimal quantity, decimal unitCost, string referenceNo, DateTime transactionDate, string? note)
    {
        if (!await _db.Companies.AnyAsync(c => c.Id == companyId))
        {
            return (false, "Company does not exist.");
        }

        if (!await _db.Products.AnyAsync(p => p.Id == productId && p.CompanyId == companyId))
        {
            return (false, "Product does not belong to this company.");
        }

        if (!await _db.Warehouses.AnyAsync(w => w.Id == warehouseId && w.CompanyId == companyId))
        {
            return (false, "Warehouse does not belong to this company.");
        }

        if (quantity <= 0)
        {
            return (false, "Stock-out quantity must be positive.");
        }

        referenceNo = string.IsNullOrWhiteSpace(referenceNo) ? string.Empty : referenceNo.Trim();
        if (string.IsNullOrWhiteSpace(referenceNo))
        {
            return (false, "Reference number is required.");
        }

        var balance = await _db.StockBalances
            .FirstOrDefaultAsync(sb => sb.ProductId == productId && sb.WarehouseId == warehouseId);

        if (balance is null || balance.Quantity <= 0)
        {
            return (false, $"Insufficient stock. Available: 0");
        }

        if (quantity > balance.Quantity)
        {
            return (false, $"Insufficient stock. Available: {balance.Quantity}");
        }

        balance.Quantity -= quantity;

        var tx = new StockTransaction
        {
            CompanyId = companyId,
            ProductId = productId,
            WarehouseId = warehouseId,
            Type = type,
            Quantity = -quantity,
            UnitCost = unitCost,
            ReferenceNo = referenceNo,
            TransactionDate = transactionDate,
            Note = note
        };

        _db.StockTransactions.Add(tx);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> TransferAsync(
        int companyId, int productId, int fromWarehouseId, int toWarehouseId,
        decimal quantity, DateTime transferDate, string? note)
    {
        if (!await _db.Companies.AnyAsync(c => c.Id == companyId))
        {
            return (false, "Company does not exist.");
        }

        if (!await _db.Products.AnyAsync(p => p.Id == productId && p.CompanyId == companyId))
        {
            return (false, "Product does not belong to this company.");
        }

        if (!await _db.Warehouses.AnyAsync(w => w.Id == fromWarehouseId && w.CompanyId == companyId))
        {
            return (false, "From warehouse does not belong to this company.");
        }

        if (!await _db.Warehouses.AnyAsync(w => w.Id == toWarehouseId && w.CompanyId == companyId))
        {
            return (false, "To warehouse does not belong to this company.");
        }

        if (fromWarehouseId == toWarehouseId)
        {
            return (false, "From warehouse and to warehouse cannot be the same.");
        }

        if (quantity <= 0)
        {
            return (false, "Transfer quantity must be positive.");
        }

        var fromBalance = await _db.StockBalances
            .FirstOrDefaultAsync(sb => sb.ProductId == productId && sb.WarehouseId == fromWarehouseId);

        if (fromBalance is null || fromBalance.Quantity <= 0)
        {
            return (false, $"Insufficient stock. Available: 0");
        }

        if (quantity > fromBalance.Quantity)
        {
            return (false, $"Insufficient stock. Available: {fromBalance.Quantity}");
        }

        fromBalance.Quantity -= quantity;

        var toBalance = await GetBalanceOrCreateAsync(productId, toWarehouseId);
        if (toBalance.Quantity == 0 && toBalance.AverageCost == 0)
        {
            toBalance.AverageCost = fromBalance.AverageCost;
        }
        else
        {
            var totalCost = (toBalance.Quantity * toBalance.AverageCost) + (quantity * fromBalance.AverageCost);
            toBalance.AverageCost = totalCost / (toBalance.Quantity + quantity);
        }
        toBalance.Quantity += quantity;

        var referenceNo = $"TRF-{DateTime.Now:yyyyMMddHHmmss}";
        var txOut = new StockTransaction
        {
            CompanyId = companyId,
            ProductId = productId,
            WarehouseId = fromWarehouseId,
            Type = StockTransactionType.StockTransfer,
            Quantity = -quantity,
            UnitCost = fromBalance.AverageCost,
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
            UnitCost = fromBalance.AverageCost,
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
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> AdjustAsync(
        int companyId, int productId, int warehouseId,
        decimal quantity, string referenceNo, DateTime transactionDate, string? note)
    {
        if (quantity == 0)
        {
            return (false, "Adjustment quantity cannot be zero.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == companyId))
        {
            return (false, "Company does not exist.");
        }

        if (!await _db.Products.AnyAsync(p => p.Id == productId && p.CompanyId == companyId))
        {
            return (false, "Product does not belong to this company.");
        }

        if (!await _db.Warehouses.AnyAsync(w => w.Id == warehouseId && w.CompanyId == companyId))
        {
            return (false, "Warehouse does not belong to this company.");
        }

        referenceNo = string.IsNullOrWhiteSpace(referenceNo) ? string.Empty : referenceNo.Trim();
        if (string.IsNullOrWhiteSpace(referenceNo))
        {
            return (false, "Reference number is required.");
        }

        var balance = await _db.StockBalances
            .FirstOrDefaultAsync(sb => sb.ProductId == productId && sb.WarehouseId == warehouseId);

        var currentQty = balance?.Quantity ?? 0;

        if (quantity < 0 && -quantity > currentQty)
        {
            return (false, $"Insufficient stock. Available: {currentQty}");
        }

        if (balance is null)
        {
            balance = new StockBalance
            {
                ProductId = productId,
                WarehouseId = warehouseId,
                Quantity = 0,
                AverageCost = 0
            };
            _db.StockBalances.Add(balance);
        }

        balance.Quantity += quantity;

        var tx = new StockTransaction
        {
            CompanyId = companyId,
            ProductId = productId,
            WarehouseId = warehouseId,
            Type = StockTransactionType.Adjustment,
            Quantity = quantity,
            UnitCost = balance.AverageCost,
            ReferenceNo = referenceNo,
            TransactionDate = transactionDate,
            Note = note
        };

        if (balance.Quantity == 0 && balance.AverageCost != 0)
        {
            balance.AverageCost = 0;
        }

        _db.StockTransactions.Add(tx);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
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
                AverageCost = 0
            };
            _db.StockBalances.Add(balance);
        }

        return balance;
    }
}
