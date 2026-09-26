using CompanyERP.ViewModels.Dashboard;

namespace CompanyERP.Interfaces.Services;

public interface IDashboardService
{
    Task<int> GetFirstCompanyIdAsync();
    Task<DashboardViewModel> GetDashboardAsync(int companyId);
}