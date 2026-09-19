using CompanyERP.Entities.Asset;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class AssetTransferController : Controller
{
    private readonly IAssetRegisterService _assetService;
    private readonly ICompanyProfileService _companyService;
    private readonly IBranchService _branchService;

    public AssetTransferController(
        IAssetRegisterService assetService,
        ICompanyProfileService companyService,
        IBranchService branchService)
    {
        _assetService = assetService;
        _companyService = companyService;
        _branchService = branchService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            return View(new List<AssetTransfer>());
        }

        var companyId = companies.First().Id;
        var transfers = await _assetService.GetAllAsync(companyId, null);
        return View(transfers.Where(a => a.Transfers.Count > 0).SelectMany(a => a.Transfers).OrderByDescending(t => t.TransferDate).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? assetId)
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            TempData["Error"] = "Create a company before transferring an asset.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = companies.First().Id;
        var model = new AssetTransfer { AssetRegisterId = assetId ?? 0, TransferDate = DateTime.Today };
        await PopulateOptionsAsync(companyId, model.AssetRegisterId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssetTransfer model)
    {
        if (!ModelState.IsValid)
        {
            var companies = await _companyService.GetAllAsync();
            await PopulateOptionsAsync(companies.Count > 0 ? companies.First().Id : 0, model.AssetRegisterId);
            return View(model);
        }

        var result = await _assetService.TransferAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            var companies = await _companyService.GetAllAsync();
            await PopulateOptionsAsync(companies.Count > 0 ? companies.First().Id : 0, model.AssetRegisterId);
            return View(model);
        }

        TempData["Success"] = "Asset transferred.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsAsync(int companyId, int selectedAssetId)
    {
        ViewBag.Assets = new SelectList(await _assetService.GetAvailableAsync(companyId), "Id", "Name", selectedAssetId);
        ViewBag.Branches = new SelectList((await _branchService.GetAllAsync()).Where(b => b.CompanyId == companyId), "Id", "Name");
    }
}