using CompanyERP.ViewModels.Reports;

namespace CompanyERP.Interfaces.Services;

public interface IInventoryReportService
{
    Task<StockReportViewModel> GetStockReportAsync(int companyId, int? categoryId, int? warehouseId);
    Task<StockMovementViewModel> GetStockMovementAsync(int companyId, DateTime? fromDate, DateTime? toDate);
    Task<LowStockViewModel> GetLowStockAsync(int companyId, int? categoryId);
    Task<WarehouseStockViewModel> GetWarehouseStockAsync(int companyId);
}