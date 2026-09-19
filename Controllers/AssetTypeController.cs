using CompanyERP.Entities.Asset;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class AssetTypeController : Controller
{
    private readonly IAssetTypeService _typeService;
    private readonly IAssetCategoryService _categoryService;
    private readonly ICompanyProfileService _companyService;

    public AssetTypeController(
        IAssetTypeService typeService,
        IAssetCategoryService categoryService,
        ICompanyProfileService companyService)
    {
        _typeService = typeService;
        _categoryService = categoryService;
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            return View(new List<AssetType>());
        }
        return View(await _typeService.GetAllAsync(companies.First().Id));
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            TempData["Error"] = "Create a company before adding an asset type.";
            return RedirectToAction(nameof(Index));
        }

        var model = new AssetType { CompanyId = companies.First().Id };
        await PopulateOptionsAsync(model.CompanyId, model.AssetCategoryId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssetType model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId, model.AssetCategoryId);
            return View(model);
        }

        var result = await _typeService.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId, model.AssetCategoryId);
            return View(model);
        }

        TempData["Success"] = "Asset type created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var model = await _typeService.GetByIdAsync(id.Value);
        if (model is null)
        {
            return NotFound();
        }

        await PopulateOptionsAsync(model.CompanyId, model.AssetCategoryId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AssetType model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId, model.AssetCategoryId);
            return View(model);
        }

        var result = await _typeService.UpdateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId, model.AssetCategoryId);
            return View(model);
        }

        TempData["Success"] = "Asset type updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var model = await _typeService.GetByIdAsync(id.Value);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _typeService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Asset type deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetTypesByCategory(int companyId, int categoryId)
    {
        var types = await _typeService.GetByCategoryAsync(companyId, categoryId);
        return Json(types.Select(t => new { t.Id, t.Name }));
    }

    private async Task PopulateOptionsAsync(int companyId, int selectedCategoryId)
    {
        var companies = await _companyService.GetAllAsync();
        ViewBag.Companies = new SelectList(companies, "Id", "Name", companyId);

        var categories = await _categoryService.GetAllAsync(companyId);
        ViewBag.Categories = new SelectList(categories, "Id", "Name", selectedCategoryId);
    }
}