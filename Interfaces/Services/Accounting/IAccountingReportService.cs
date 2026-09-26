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
    Task<GeneralJournalViewModel> GetGeneralJournalAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null, int? branchId = null);
    Task<ComparativePandLViewModel> GetComparativePandLAsync(int companyId, DateTime? monthDate);
    Task<CashFlowViewModel> GetCashFlowAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<BankCashAccountSummaryViewModel> GetBankCashSummaryAsync(int companyId);
    Task<CoaReportViewModel> GetCoaReportAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<VoucherViewModel> GetJournalVoucherAsync(int companyId, int journalEntryId);
    Task<PaymentVoucherViewModel> GetPaymentVoucherAsync(int companyId, int paymentId);
}