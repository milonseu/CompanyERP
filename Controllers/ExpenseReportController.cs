using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class ExpenseReportController : Controller
{
    private readonly IExpenseReportService _reportService;
    private readonly ICompanyProfileService _companyService;
    private readonly IExpenseTypeService _expenseTypeService;

    public ExpenseReportController(
        IExpenseReportService reportService,
        ICompanyProfileService companyService,
        IExpenseTypeService expenseTypeService)
    {
        _reportService = reportService;
        _companyService = companyService;
        _expenseTypeService = expenseTypeService;
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
    public async Task<IActionResult> Summary(string? fromDate, string? toDate, int? expenseTypeId)
    {
        var companyId = await GetCompanyIdAsync();
        var vm = await _reportService.GetSummaryAsync(companyId, ParseDate(fromDate), ParseDate(toDate), expenseTypeId);
        ViewBag.ExpenseTypes = new SelectList(
            (await _expenseTypeService.GetAllAsync()).Where(e => e.CompanyId == companyId),
            "Id", "Name", expenseTypeId);
        return View(vm);
    }
}