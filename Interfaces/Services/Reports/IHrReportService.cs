using CompanyERP.ViewModels.Reports;

namespace CompanyERP.Interfaces.Services;

public interface IHrReportService
{
    Task<SalarySummaryViewModel> GetSalarySummaryAsync(int companyId, DateTime? fromDate, DateTime? toDate, int? departmentId);
    Task<SalaryRegisterViewModel> GetSalaryRegisterAsync(int companyId, DateTime? fromDate, DateTime? toDate, int? departmentId);
    Task<AssetCustodyViewModel> GetAssetCustodyAsync(int companyId);
    Task<EmployeeListViewModel> GetEmployeeListAsync(int companyId, int? departmentId);
}