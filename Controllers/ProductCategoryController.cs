using CompanyERP.Entities.Inventory;
using CompanyERP.Interfaces.Services;
using CompanyERP.Services.Company;
using CompanyERP.Services.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

[HasPermission("Inventory.View")]
public class ProductCategoryController : Controller
{
    private readonly IProductCategoryService _categoryService;
    private readonly ICompanyProfileService _companyService;

    public ProductCategoryController(IProductCategoryService categoryService, ICompanyProfileService companyService)
    {
        _categoryService = categoryService;
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var categories = await _categoryService.GetAllAsync();
        return View(categories);
    }

    [HttpGet]
    [HasPermission("Inventory.Create")]
    public async Task<IActionResult> Create()
    {
        if (!await HasCompaniesAsync())
        {
            TempData["Error"] = "Create a company before adding a product category.";
            return RedirectToAction(nameof(Index));
        }

        return View(new ProductCategory());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Inventory.Create")]
    public async Task<IActionResult> Create(ProductCategory model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _categoryService.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        TempData["Success"] = "Product category created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [HasPermission("Inventory.Edit")]
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
            TempData["Error"] = "Create a company before editing a product category.";
            return RedirectToAction(nameof(Index));
        }

        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Inventory.Edit")]
    public async Task<IActionResult> Edit(int id, ProductCategory model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _categoryService.UpdateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        TempData["Success"] = "Product category updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [HasPermission("Inventory.Delete")]
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
    [HasPermission("Inventory.Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _categoryService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Product category deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> HasCompaniesAsync()
    {
        var companies = await _companyService.GetAllAsync();
        return companies.Count > 0;
    }
}