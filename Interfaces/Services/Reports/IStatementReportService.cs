using CompanyERP.ViewModels.Accounting;

namespace CompanyERP.Interfaces.Services;

public interface IStatementReportService
{
    Task<StatementViewModel> GetCustomerStatementAsync(int companyId, int customerId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<StatementViewModel> GetSupplierStatementAsync(int companyId, int supplierId, DateTime? fromDate = null, DateTime? toDate = null);
}