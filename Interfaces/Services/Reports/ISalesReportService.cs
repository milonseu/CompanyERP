using CompanyERP.ViewModels.Reports;

namespace CompanyERP.Interfaces.Services;

public interface ISalesReportService
{
    Task<SalesSummaryViewModel> GetSummaryAsync(int companyId, DateTime? fromDate, DateTime? toDate);
    Task<SalesByProductViewModel> GetByProductAsync(int companyId, DateTime? fromDate, DateTime? toDate);
    Task<OutstandingReceivableViewModel> GetOutstandingAsync(int companyId, DateTime? asOfDate);
    Task<TopSalesViewModel> GetTopAsync(int companyId, DateTime? fromDate, DateTime? toDate, int top = 10);
    Task<ServiceCompletionViewModel> GetServiceCompletionAsync(int companyId, DateTime? fromDate, DateTime? toDate);
}