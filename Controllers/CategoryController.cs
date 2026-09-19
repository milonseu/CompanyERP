using CompanyERP.Entities.MasterData;
using CompanyERP.Interfaces.Services;
using CompanyERP.Services.Company;
using CompanyERP.Services.MasterData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class CategoryController : Controller
{
    private readonly ICategoryService _categoryService;
    private readonly ICategoryTypeService _categoryTypeService;
    private readonly ICompanyProfileService _companyService;

    public CategoryController(ICategoryService categoryService, ICategoryTypeService categoryTypeService, ICompanyProfileService companyService)
    {
        _categoryService = categoryService;
        _categoryTypeService = categoryTypeService;
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? companyId, int? categoryTypeId, bool? showInactive)
    {
        var all = await _categoryService.GetAllAsync();
        var categories = all
            .Where(c => !companyId.HasValue || c.CompanyId == companyId.Value)
            .Where(c => !categoryTypeId.HasValue || c.CategoryTypeId == categoryTypeId.Value)
            .Where(c => showInactive == true || c.IsActive)
            .ToList();

        ViewBag.Companies = new SelectList(await _companyService.GetAllAsync(), "Id", "Name", companyId);

        var types = companyId.HasValue
            ? await _categoryTypeService.GetByCompanyIdAsync(companyId.Value)
            : new List<CategoryType>();
        ViewBag.CategoryTypes = new SelectList(types, "Id", "Name", categoryTypeId);

        ViewBag.SelectedCompanyId = companyId;
        ViewBag.SelectedCategoryTypeId = categoryTypeId;
        ViewBag.ShowInactive = showInactive == true;

        return View(categories);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!await HasCompaniesAsync())
        {
            TempData["Error"] = "Create a company before adding a category.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateOptionsAsync();
        return View(new Category());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId, model.CategoryTypeId);
            return View(model);
        }

        var result = await _categoryService.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId, model.CategoryTypeId);
            return View(model);
        }

        TempData["Success"] = "Category created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var category = await _categoryService.GetByIdAsync(id.Value);
        if (category is null)
        {
            return NotFound();
        }

        if (!await HasCompaniesAsync())
        {
            TempData["Error"] = "Create a company before editing a category.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateOptionsAsync(category.CompanyId, category.CategoryTypeId);
        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId, model.CategoryTypeId);
            return View(model);
        }

        var result = await _categoryService.UpdateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId, model.CategoryTypeId);
            return View(model);
        }

        TempData["Success"] = "Category updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var category = await _categoryService.GetByIdAsync(id.Value);
        if (category is null)
        {
            return NotFound();
        }

        return View(category);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _categoryService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Category deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetCategoriesByType(int companyId, int categoryTypeId)
    {
        var categories = await _categoryService.GetByCompanyAndTypeAsync(companyId, categoryTypeId);
        return Json(categories.Select(c => new { c.Id, c.Name }));
    }

    private async Task<bool> HasCompaniesAsync()
    {
        var companies = await _companyService.GetAllAsync();
        return companies.Count > 0;
    }

    private async Task PopulateOptionsAsync(int? companyId = null, int? categoryTypeId = null)
    {
        var companies = await _companyService.GetAllAsync();
        ViewBag.Companies = new SelectList(companies, "Id", "Name", companyId);

        var types = companyId.HasValue
            ? await _categoryTypeService.GetByCompanyIdAsync(companyId.Value)
            : new List<CategoryType>();
        ViewBag.CategoryTypes = new SelectList(types, "Id", "Name", categoryTypeId);
    }
}