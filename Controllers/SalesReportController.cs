using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace CompanyERP.Controllers;

public class SalesReportController : Controller
{
    private readonly ISalesReportService _reportService;
    private readonly ICompanyProfileService _companyService;

    public SalesReportController(ISalesReportService reportService, ICompanyProfileService companyService)
    {
        _reportService = reportService;
        _companyService = companyService;
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
    public async Task<IActionResult> Summary(string? fromDate, string? toDate)
    {
        var vm = await _reportService.GetSummaryAsync(await GetCompanyIdAsync(), ParseDate(fromDate), ParseDate(toDate));
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ByProduct(string? fromDate, string? toDate)
    {
        var vm = await _reportService.GetByProductAsync(await GetCompanyIdAsync(), ParseDate(fromDate), ParseDate(toDate));
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Outstanding(string? asOfDate)
    {
        var vm = await _reportService.GetOutstandingAsync(await GetCompanyIdAsync(), ParseDate(asOfDate));
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Top(string? fromDate, string? toDate, int top = 10)
    {
        var vm = await _reportService.GetTopAsync(await GetCompanyIdAsync(), ParseDate(fromDate), ParseDate(toDate), top);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ServiceCompletion(string? fromDate, string? toDate)
    {
        var vm = await _reportService.GetServiceCompletionAsync(await GetCompanyIdAsync(), ParseDate(fromDate), ParseDate(toDate));
        return View(vm);
    }
}