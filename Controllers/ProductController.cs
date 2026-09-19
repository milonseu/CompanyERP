using CompanyERP.Entities.Inventory;
using CompanyERP.Interfaces.Services;
using CompanyERP.Services.Company;
using CompanyERP.Services.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class ProductController : Controller
{
    private readonly IProductService _productService;
    private readonly IProductCategoryService _categoryService;
    private readonly ICompanyProfileService _companyService;

    public ProductController(IProductService productService, IProductCategoryService categoryService, ICompanyProfileService companyService)
    {
        _productService = productService;
        _categoryService = categoryService;
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var products = await _productService.GetAllAsync();
        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!await HasCompaniesAsync())
        {
            TempData["Error"] = "Create a company before adding a product.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateOptionsAsync();
        return View(new Product());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId);
            return View(model);
        }

        var result = await _productService.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId);
            return View(model);
        }

        TempData["Success"] = "Product created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var product = await _productService.GetByIdAsync(id.Value);
        if (product is null)
        {
            return NotFound();
        }

        if (!await HasCompaniesAsync())
        {
            TempData["Error"] = "Create a company before editing a product.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateOptionsAsync(product.CompanyId, product.CategoryId);
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId, model.CategoryId);
            return View(model);
        }

        var result = await _productService.UpdateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId, model.CategoryId);
            return View(model);
        }

        TempData["Success"] = "Product updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var product = await _productService.GetByIdAsync(id.Value);
        if (product is null)
        {
            return NotFound();
        }

        return View(product);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var product = await _productService.GetByIdAsync(id.Value);
        if (product is null)
        {
            return NotFound();
        }

        return View(product);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _productService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Product deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> HasCompaniesAsync()
    {
        var companies = await _companyService.GetAllAsync();
        return companies.Count > 0;
    }

    private async Task PopulateOptionsAsync(int? companyId = null, int? categoryId = null)
    {
        var companies = await _companyService.GetAllAsync();
        ViewBag.Companies = new SelectList(companies, "Id", "Name", companyId);

        var categories = companyId.HasValue
            ? await _categoryService.GetByCompanyIdAsync(companyId.Value)
            : new List<ProductCategory>();
        ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);
    }
}