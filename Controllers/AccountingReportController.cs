using CompanyERP.Interfaces.Services;
using CompanyERP.ViewModels.Accounting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class AccountingReportController : Controller
{
    private readonly IAccountingReportService _reportService;
    private readonly IChartOfAccountService _accountService;
    private readonly ICompanyProfileService _companyService;
    private readonly IBranchService _branchService;

    public AccountingReportController(
        IAccountingReportService reportService,
        IChartOfAccountService accountService,
        ICompanyProfileService companyService,
        IBranchService branchService)
    {
        _reportService = reportService;
        _accountService = accountService;
        _companyService = companyService;
        _branchService = branchService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    private async Task<int> GetCompanyIdAsync()
    {
        var companies = await _companyService.GetAllAsync();
        return companies.Count == 0 ? 0 : companies.First().Id;
    }

    private async Task<DateTime?> ParseDateAsync(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParse(value, out var parsed) ? parsed : null;
    }

    [HttpGet]
    public async Task<IActionResult> TrialBalance(string? fromDate, string? toDate)
    {
        var vm = await _reportService.GetTrialBalanceAsync(
            await GetCompanyIdAsync(),
            await ParseDateAsync(fromDate),
            await ParseDateAsync(toDate));
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Ledger(int? accountId, string? fromDate, string? toDate, int? branchId)
    {
        var companyId = await GetCompanyIdAsync();
        var vm = await _reportService.GetLedgerAsync(
            companyId,
            accountId,
            await ParseDateAsync(fromDate),
            await ParseDateAsync(toDate),
            branchId);
        ViewBag.Accounts = new SelectList(await _accountService.GetPostableAsync(companyId), "Id", "AccountDisplayLabel", accountId);
        ViewBag.Branches = new SelectList(
            (await _branchService.GetAllAsync()).Where(b => b.CompanyId == companyId),
            "Id", "Name", branchId);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> CashBook(string? fromDate, string? toDate)
    {
        var vm = await _reportService.GetCashBookAsync(
            await GetCompanyIdAsync(),
            await ParseDateAsync(fromDate),
            await ParseDateAsync(toDate));
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> BankBook(string? fromDate, string? toDate)
    {
        var vm = await _reportService.GetBankBookAsync(
            await GetCompanyIdAsync(),
            await ParseDateAsync(fromDate),
            await ParseDateAsync(toDate));
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ProfitAndLoss(string? fromDate, string? toDate)
    {
        var vm = await _reportService.GetProfitAndLossAsync(
            await GetCompanyIdAsync(),
            await ParseDateAsync(fromDate),
            await ParseDateAsync(toDate));
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> BalanceSheet(string? asOfDate)
    {
        var vm = await _reportService.GetBalanceSheetAsync(
            await GetCompanyIdAsync(),
            await ParseDateAsync(asOfDate));
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ReceivablesPayables()
    {
        var vm = await _reportService.GetReceivablesPayablesAsync(await GetCompanyIdAsync());
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> GeneralJournal(string? fromDate, string? toDate, int? branchId)
    {
        var companyId = await GetCompanyIdAsync();
        var vm = await _reportService.GetGeneralJournalAsync(companyId, await ParseDateAsync(fromDate), await ParseDateAsync(toDate), branchId);
        ViewBag.Branches = new SelectList(
            (await _branchService.GetAllAsync()).Where(b => b.CompanyId == companyId),
            "Id", "Name", branchId);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ComparativePAndL(string? month)
    {
        var vm = await _reportService.GetComparativePandLAsync(await GetCompanyIdAsync(), await ParseDateAsync(month));
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> CashFlow(string? fromDate, string? toDate)
    {
        var vm = await _reportService.GetCashFlowAsync(await GetCompanyIdAsync(), await ParseDateAsync(fromDate), await ParseDateAsync(toDate));
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> BankCashSummary()
    {
        var vm = await _reportService.GetBankCashSummaryAsync(await GetCompanyIdAsync());
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> CoaReport(string? fromDate, string? toDate)
    {
        var vm = await _reportService.GetCoaReportAsync(await GetCompanyIdAsync(), await ParseDateAsync(fromDate), await ParseDateAsync(toDate));
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> JournalVoucher(int id)
    {
        var vm = await _reportService.GetJournalVoucherAsync(await GetCompanyIdAsync(), id);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> PaymentVoucher(int id)
    {
        var vm = await _reportService.GetPaymentVoucherAsync(await GetCompanyIdAsync(), id);
        return View(vm);
    }
}