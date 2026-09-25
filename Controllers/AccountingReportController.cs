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
        ViewBag.Accounts = new SelectList(await _accountService.GetActiveAsync(companyId), "Id", "AccountDisplayLabel", accountId);
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
}