using CompanyERP.ViewModels.Reports;

namespace CompanyERP.Interfaces.Services;

public interface IPurchaseReportService
{
    Task<PurchaseSummaryViewModel> GetSummaryAsync(int companyId, DateTime? fromDate, DateTime? toDate);
    Task<ProcurementAgingViewModel> GetProcurementAgingAsync(int companyId);
    Task<TopSuppliersViewModel> GetTopSuppliersAsync(int companyId, DateTime? fromDate, DateTime? toDate, int top = 10);
}