using CompanyERP.Data;
using CompanyERP.Entities.Inventory;
using CompanyERP.Interfaces.Services;
using CompanyERP.ViewModels.Reports;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Reports;

public class InventoryReportService : IInventoryReportService
{
    private readonly ApplicationDbContext _db;

    public InventoryReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<StockReportViewModel> GetStockReportAsync(int companyId, int? categoryId, int? warehouseId)
    {
        var balances = await _db.StockBalances
            .AsNoTracking()
            .Include(b => b.Product)
                .ThenInclude(p => p!.Category)
            .Include(b => b.Warehouse)
            .Where(b => b.Product != null && b.Product.CompanyId == companyId)
            .ToListAsync();

        var rows = balances
            .Where(b => (!categoryId.HasValue || b.Product!.CategoryId == categoryId.Value) &&
                        (!warehouseId.HasValue || b.WarehouseId == warehouseId.Value))
            .Select(b => new StockRow
            {
                Code = b.Product!.Code,
                Product = b.Product.Name,
                Category = b.Product.Category?.Name ?? "",
                Warehouse = b.Warehouse?.Name ?? "",
                Quantity = b.Quantity,
                AverageCost = Math.Round(b.AverageCost, 4),
                Value = Math.Round(b.Quantity * b.AverageCost, 2),
                MinimumStockLevel = b.Product.MinimumStockLevel
            })
            .OrderBy(r => r.Code)
            .ThenBy(r => r.Warehouse)
            .ToList();

        return new StockReportViewModel
        {
            CategoryId = categoryId,
            WarehouseId = warehouseId,
            Rows = rows,
            TotalQuantity = Math.Round(rows.Sum(r => r.Quantity), 2),
            TotalValue = Math.Round(rows.Sum(r => r.Value), 2)
        };
    }

    public async Task<StockMovementViewModel> GetStockMovementAsync(int companyId, DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate?.Date ?? DateTime.MinValue.Date;
        var to = toDate?.Date ?? DateTime.MaxValue.Date;

        var rows = await _db.StockTransactions
            .AsNoTracking()
            .Include(t => t.Product)
            .Include(t => t.Warehouse)
            .Where(t => t.CompanyId == companyId && t.TransactionDate >= from && t.TransactionDate <= to)
            .OrderByDescending(t => t.TransactionDate)
            .ThenBy(t => t.ReferenceNo)
            .ToListAsync();

        var movements = rows.Select(t => new StockMovementRow
        {
            Code = t.Product?.Code ?? "",
            Product = t.Product?.Name ?? "",
            Warehouse = t.Warehouse?.Name ?? "",
            Date = t.TransactionDate,
            Type = t.Type.ToString(),
            Reference = t.ReferenceNo,
            Quantity = Math.Abs(t.Quantity),
            UnitCost = t.UnitCost,
            Value = Math.Round(Math.Abs(t.Quantity) * t.UnitCost, 2)
        }).ToList();

        var allTransactions = await _db.StockTransactions
            .AsNoTracking()
            .Include(t => t.Product)
            .Where(t => t.CompanyId == companyId)
            .ToListAsync();

        var products = allTransactions
            .GroupBy(t => t.ProductId)
            .Select(g =>
            {
                var product = g.First().Product;
                var opening = g.Where(t => t.TransactionDate < from).Sum(t => t.Quantity);
                var inQty = g.Where(t => t.TransactionDate >= from && t.TransactionDate <= to && t.Quantity >= 0).Sum(t => t.Quantity);
                var outQty = g.Where(t => t.TransactionDate >= from && t.TransactionDate <= to && t.Quantity < 0).Sum(t => Math.Abs(t.Quantity));
                return new StockMovementProductRow
                {
                    Code = product?.Code ?? "",
                    Product = product?.Name ?? "",
                    OpeningQty = opening,
                    In = inQty,
                    Out = outQty,
                    ClosingQty = opening + inQty - outQty
                };
            })
            .OrderBy(r => r.Code)
            .ToList();

        return new StockMovementViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            Rows = movements,
            Products = products,
            TotalIn = Math.Round(rows.Where(r => r.Quantity >= 0).Sum(r => Math.Abs(r.Quantity)), 2),
            TotalOut = Math.Round(rows.Where(r => r.Quantity < 0).Sum(r => Math.Abs(r.Quantity)), 2)
        };
    }

    public async Task<LowStockViewModel> GetLowStockAsync(int companyId, int? categoryId)
    {
        var balances = await _db.StockBalances
            .AsNoTracking()
            .Include(b => b.Product)
                .ThenInclude(p => p!.Category)
            .Include(b => b.Warehouse)
            .Where(b => b.Product != null && b.Product.CompanyId == companyId)
            .ToListAsync();

        var rows = balances
            .Where(b => b.Quantity <= (b.Product?.MinimumStockLevel ?? 0) &&
                        (!categoryId.HasValue || b.Product!.CategoryId == categoryId.Value))
            .Select(b => new LowStockRow
            {
                Code = b.Product!.Code,
                Product = b.Product.Name,
                Category = b.Product.Category?.Name ?? "",
                Warehouse = b.Warehouse?.Name ?? "",
                OnHand = b.Quantity,
                MinimumStockLevel = b.Product.MinimumStockLevel,
                Shortfall = Math.Max(0, b.Product.MinimumStockLevel - b.Quantity)
            })
            .OrderBy(r => r.Code)
            .ThenBy(r => r.Warehouse)
            .ToList();

        return new LowStockViewModel
        {
            CategoryId = categoryId,
            Rows = rows,
            Count = rows.Count
        };
    }

    public async Task<WarehouseStockViewModel> GetWarehouseStockAsync(int companyId)
    {
        var balances = await _db.StockBalances
            .AsNoTracking()
            .Include(b => b.Warehouse)
            .Include(b => b.Product)
            .Where(b => b.Product != null && b.Product.CompanyId == companyId)
            .ToListAsync();

        var rows = balances
            .GroupBy(b => b.WarehouseId)
            .Select(g => new WarehouseStockRow
            {
                Warehouse = g.First().Warehouse?.Name ?? "",
                Products = g.Count(),
                Quantity = Math.Round(g.Sum(b => b.Quantity), 2),
                Value = Math.Round(g.Sum(b => b.Quantity * b.AverageCost), 2)
            })
            .OrderBy(r => r.Warehouse)
            .ToList();

        return new WarehouseStockViewModel
        {
            Rows = rows,
            TotalQuantity = Math.Round(rows.Sum(r => r.Quantity), 2),
            TotalValue = Math.Round(rows.Sum(r => r.Value), 2)
        };
    }
}