using CompanyERP.Entities.MasterData;
using CompanyERP.Interfaces.Services;
using CompanyERP.Services.Company;
using CompanyERP.Services.MasterData;
using Microsoft.AspNetCore.Mvc;

namespace CompanyERP.Controllers;

public class CategoryTypeController : Controller
{
    private readonly ICategoryTypeService _categoryTypeService;
    private readonly ICompanyProfileService _companyService;

    public CategoryTypeController(ICategoryTypeService categoryTypeService, ICompanyProfileService companyService)
    {
        _categoryTypeService = categoryTypeService;
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? companyId, bool? showInactive)
    {
        var all = await _categoryTypeService.GetAllAsync();
        var types = all
            .Where(t => !companyId.HasValue || t.CompanyId == companyId.Value)
            .Where(t => showInactive == true || t.IsActive)
            .ToList();

        ViewBag.Companies = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            await _companyService.GetAllAsync(), "Id", "Name", companyId);
        ViewBag.SelectedCompanyId = companyId;
        ViewBag.ShowInactive = showInactive == true;

        return View(types);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!await HasCompaniesAsync())
        {
            TempData["Error"] = "Create a company before adding a category type.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Companies = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            await _companyService.GetAllAsync(), "Id", "Name");
        return View(new CategoryType());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryType model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Companies = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _companyService.GetAllAsync(), "Id", "Name", model.CompanyId);
            return View(model);
        }

        var result = await _categoryTypeService.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            ViewBag.Companies = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _companyService.GetAllAsync(), "Id", "Name", model.CompanyId);
            return View(model);
        }

        TempData["Success"] = "Category type created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var categoryType = await _categoryTypeService.GetByIdAsync(id.Value);
        if (categoryType is null)
        {
            return NotFound();
        }

        if (!await HasCompaniesAsync())
        {
            TempData["Error"] = "Create a company before editing a category type.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Companies = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            await _companyService.GetAllAsync(), "Id", "Name", categoryType.CompanyId);
        return View(categoryType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryType model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _companyService.GetAllAsync(), "Id", "Name", model.CompanyId);
            return View(model);
        }

        var result = await _categoryTypeService.UpdateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            ViewBag.Companies = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _companyService.GetAllAsync(), "Id", "Name", model.CompanyId);
            return View(model);
        }

        TempData["Success"] = "Category type updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetTypesByCompany(int companyId)
    {
        var types = await _categoryTypeService.GetByCompanyIdAsync(companyId);
        return Json(types.Select(t => new { t.Id, t.Name }));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var categoryType = await _categoryTypeService.GetByIdAsync(id.Value);
        if (categoryType is null)
        {
            return NotFound();
        }

        return View(categoryType);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _categoryTypeService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Category type deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> HasCompaniesAsync()
    {
        var companies = await _companyService.GetAllAsync();
        return companies.Count > 0;
    }
}