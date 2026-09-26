using CompanyERP.ViewModels.Reports;

namespace CompanyERP.Interfaces.Services;

public interface IPaymentReportService
{
    Task<PaymentSummaryViewModel> GetSummaryAsync(int companyId, DateTime? fromDate, DateTime? toDate, int? branchId);
    Task<SalesVsPaymentViewModel> GetSalesVsPaymentAsync(int companyId, DateTime? fromDate, DateTime? toDate);
    Task<PurchasesVsPaymentViewModel> GetPurchasesVsPaymentAsync(int companyId, DateTime? fromDate, DateTime? toDate);
}