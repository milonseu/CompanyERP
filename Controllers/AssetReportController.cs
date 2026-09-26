using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class AssetReportController : Controller
{
    private readonly IAssetReportService _reportService;
    private readonly ICompanyProfileService _companyService;
    private readonly IAssetTypeService _assetTypeService;

    public AssetReportController(
        IAssetReportService reportService,
        ICompanyProfileService companyService,
        IAssetTypeService assetTypeService)
    {
        _reportService = reportService;
        _companyService = companyService;
        _assetTypeService = assetTypeService;
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
    public async Task<IActionResult> Register(string? asOfDate, int? assetTypeId)
    {
        var companyId = await GetCompanyIdAsync();
        var vm = await _reportService.GetRegisterAsync(companyId, ParseDate(asOfDate), assetTypeId);
        ViewBag.AssetTypes = new SelectList(
            (await _assetTypeService.GetAllAsync(companyId)),
            "Id", "Name", assetTypeId);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Depreciation(string? fromPeriod, string? toPeriod)
    {
        var vm = await _reportService.GetDepreciationScheduleAsync(await GetCompanyIdAsync(), fromPeriod, toPeriod);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Disposals()
    {
        var vm = await _reportService.GetDisposalsAsync(await GetCompanyIdAsync());
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Groups()
    {
        var vm = await _reportService.GetGroupSummaryAsync(await GetCompanyIdAsync());
        return View(vm);
    }
}