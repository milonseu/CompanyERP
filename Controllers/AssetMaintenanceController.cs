using CompanyERP.Entities.Asset;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class AssetMaintenanceController : Controller
{
    private readonly IAssetMaintenanceService _maintenanceService;
    private readonly IAssetRegisterService _assetService;
    private readonly ICompanyProfileService _companyService;
    private readonly IBranchService _branchService;

    public AssetMaintenanceController(
        IAssetMaintenanceService maintenanceService,
        IAssetRegisterService assetService,
        ICompanyProfileService companyService,
        IBranchService branchService)
    {
        _maintenanceService = maintenanceService;
        _assetService = assetService;
        _companyService = companyService;
        _branchService = branchService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? assetId)
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            return View(new List<AssetMaintenance>());
        }

        var companyId = companies.First().Id;
        ViewBag.Assets = new SelectList(await _assetService.GetAvailableAsync(companyId), "Id", "Name", assetId);
        return View(await _maintenanceService.GetAllAsync(companyId, assetId));
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? assetId)
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            TempData["Error"] = "Create a company before recording maintenance.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = companies.First().Id;
        var model = new AssetMaintenance { AssetRegisterId = assetId ?? 0, MaintenanceDate = DateTime.Today };
        await PopulateOptionsAsync(companyId, model.AssetRegisterId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssetMaintenance model)
    {
        if (!ModelState.IsValid)
        {
            var companies = await _companyService.GetAllAsync();
            await PopulateOptionsAsync(companies.Count > 0 ? companies.First().Id : 0, model.AssetRegisterId);
            return View(model);
        }

        var result = await _maintenanceService.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            var companies = await _companyService.GetAllAsync();
            await PopulateOptionsAsync(companies.Count > 0 ? companies.First().Id : 0, model.AssetRegisterId);
            return View(model);
        }

        TempData["Success"] = "Maintenance record created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _maintenanceService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Maintenance record deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsAsync(int companyId, int selectedAssetId)
    {
        ViewBag.Assets = new SelectList(await _assetService.GetAvailableAsync(companyId), "Id", "Name", selectedAssetId);
    }
}