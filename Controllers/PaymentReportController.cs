using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class PaymentReportController : Controller
{
    private readonly IPaymentReportService _reportService;
    private readonly ICompanyProfileService _companyService;
    private readonly IBranchService _branchService;

    public PaymentReportController(
        IPaymentReportService reportService,
        ICompanyProfileService companyService,
        IBranchService branchService)
    {
        _reportService = reportService;
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

    private DateTime? ParseDate(string? value)
    {
        return DateTime.TryParse(value, out var parsed) ? parsed : null;
    }

    [HttpGet]
    public async Task<IActionResult> Summary(string? fromDate, string? toDate, int? branchId)
    {
        var companyId = await GetCompanyIdAsync();
        var vm = await _reportService.GetSummaryAsync(companyId, ParseDate(fromDate), ParseDate(toDate), branchId);
        ViewBag.Branches = new SelectList(
            (await _branchService.GetAllAsync()).Where(b => b.CompanyId == companyId),
            "Id", "Name", branchId);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> SalesVsPayment(string? fromDate, string? toDate)
    {
        var vm = await _reportService.GetSalesVsPaymentAsync(await GetCompanyIdAsync(), ParseDate(fromDate), ParseDate(toDate));
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> PurchasesVsPayment(string? fromDate, string? toDate)
    {
        var vm = await _reportService.GetPurchasesVsPaymentAsync(await GetCompanyIdAsync(), ParseDate(fromDate), ParseDate(toDate));
        return View(vm);
    }
}