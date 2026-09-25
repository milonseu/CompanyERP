using CompanyERP.Data;
using CompanyERP.Entities.Accounting;
using CompanyERP.ViewModels.Accounting;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Interfaces.Services;

public interface IAccountingReportService
{
    Task<TrialBalanceViewModel> GetTrialBalanceAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<LedgerViewModel> GetLedgerAsync(int companyId, int? accountId = null, DateTime? fromDate = null, DateTime? toDate = null, int? branchId = null);
    Task<CashBookViewModel> GetCashBookAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<CashBookViewModel> GetBankBookAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<ProfitAndLossViewModel> GetProfitAndLossAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<BalanceSheetViewModel> GetBalanceSheetAsync(int companyId, DateTime? asOfDate = null);
    Task<ReceivablesPayablesViewModel> GetReceivablesPayablesAsync(int companyId);
}