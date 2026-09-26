using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace CompanyERP.Controllers;

public class PurchaseReportController : Controller
{
    private readonly IPurchaseReportService _reportService;
    private readonly ICompanyProfileService _companyService;

    public PurchaseReportController(IPurchaseReportService reportService, ICompanyProfileService companyService)
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
}