using CompanyERP.Entities.Asset;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class AssetDisposalController : Controller
{
    private readonly IAssetDisposalService _disposalService;
    private readonly IAssetRegisterService _assetService;
    private readonly ICompanyProfileService _companyService;

    public AssetDisposalController(
        IAssetDisposalService disposalService,
        IAssetRegisterService assetService,
        ICompanyProfileService companyService)
    {
        _disposalService = disposalService;
        _assetService = assetService;
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            return View(new List<AssetDisposal>());
        }

        return View(await _disposalService.GetAllAsync(companies.First().Id));
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? assetId)
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            TempData["Error"] = "Create a company before disposing an asset.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = companies.First().Id;
        var model = new AssetDisposal { AssetRegisterId = assetId ?? 0, DisposalDate = DateTime.Today };
        await PopulateOptionsAsync(companyId, model.AssetRegisterId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssetDisposal model)
    {
        if (!ModelState.IsValid)
        {
            var companies = await _companyService.GetAllAsync();
            await PopulateOptionsAsync(companies.Count > 0 ? companies.First().Id : 0, model.AssetRegisterId);
            return View(model);
        }

        var result = await _disposalService.DisposeAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            var companies = await _companyService.GetAllAsync();
            await PopulateOptionsAsync(companies.Count > 0 ? companies.First().Id : 0, model.AssetRegisterId);
            return View(model);
        }

        TempData["Success"] = "Asset disposed successfully.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var model = await _disposalService.GetByIdAsync(id.Value);
        return model is null ? NotFound() : View(model);
    }

    private async Task PopulateOptionsAsync(int companyId, int selectedAssetId)
    {
        ViewBag.Assets = new SelectList(await _assetService.GetAvailableAsync(companyId, includeDisposed: true), "Id", "Name", selectedAssetId);
    }
}