using CompanyERP.ViewModels.Accounting;

namespace CompanyERP.Interfaces.Services;

public interface IAgingReportService
{
    Task<AgingReportViewModel> GetReceivablesAsync(int companyId, DateTime asOfDate, int? branchId = null, int? partyId = null);
    Task<AgingReportViewModel> GetPayablesAsync(int companyId, DateTime asOfDate, int? branchId = null, int? partyId = null);
}