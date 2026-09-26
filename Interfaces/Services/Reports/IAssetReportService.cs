using CompanyERP.ViewModels.Reports;

namespace CompanyERP.Interfaces.Services;

public interface IAssetReportService
{
    Task<AssetReportViewModel> GetRegisterAsync(int companyId, DateTime? asOfDate, int? assetTypeId);
    Task<DepreciationScheduleViewModel> GetDepreciationScheduleAsync(int companyId, string? fromPeriod, string? toPeriod);
    Task<DisposalSummaryViewModel> GetDisposalsAsync(int companyId);
    Task<AssetGroupSummaryViewModel> GetGroupSummaryAsync(int companyId);
}