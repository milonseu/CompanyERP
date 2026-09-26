using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class HrReportController : Controller
{
    private readonly IHrReportService _reportService;
    private readonly ICompanyProfileService _companyService;
    private readonly IDepartmentService _departmentService;

    public HrReportController(
        IHrReportService reportService,
        ICompanyProfileService companyService,
        IDepartmentService departmentService)
    {
        _reportService = reportService;
        _companyService = companyService;
        _departmentService = departmentService;
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
    public async Task<IActionResult> Salary(string? fromDate, string? toDate, int? departmentId)
    {
        var companyId = await GetCompanyIdAsync();
        var vm = await _reportService.GetSalarySummaryAsync(companyId, ParseDate(fromDate), ParseDate(toDate), departmentId);
        ViewBag.Departments = new SelectList(
            (await _departmentService.GetAllAsync()).Where(d => d.CompanyId == companyId),
            "Id", "Name", departmentId);
        return View(vm);
    }
}