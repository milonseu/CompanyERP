using CompanyERP.Entities.Asset;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class AssetDepreciationController : Controller
{
    private readonly IAssetDepreciationService _depreciationService;
    private readonly ICompanyProfileService _companyService;

    public AssetDepreciationController(IAssetDepreciationService depreciationService, ICompanyProfileService companyService)
    {
        _depreciationService = depreciationService;
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? periodKey)
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            return View(new List<AssetDepreciation>());
        }

        var companyId = companies.First().Id;
        if (string.IsNullOrWhiteSpace(periodKey))
        {
            periodKey = DateTime.Today.ToString("yyyy-MM");
        }

        ViewBag.PeriodKey = periodKey;
        ViewBag.Periods = new SelectList(await _depreciationService.GetPeriodsAsync(companyId), periodKey);
        return View(await _depreciationService.GetAllAsync(companyId, periodKey));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Run(string periodKey, string? note)
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            TempData["Error"] = "Create a company before running depreciation.";
            return RedirectToAction(nameof(Index));
        }

        periodKey = string.IsNullOrWhiteSpace(periodKey) ? DateTime.Today.ToString("yyyy-MM") : periodKey.Trim();
        var result = await _depreciationService.RunAsync(companies.First().Id, periodKey, note);

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = result.Count > 0
                ? $"Depreciation posted for {result.Count} asset(s), total {result.Total:N2}."
                : "No asset required depreciation for this period.";
        }

        return RedirectToAction(nameof(Index), new { periodKey });
    }
}