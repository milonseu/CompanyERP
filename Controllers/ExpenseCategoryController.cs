using CompanyERP.Entities.Expense;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class ExpenseCategoryController : Controller
{
    private readonly IExpenseCategoryService _categoryService;
    private readonly ICompanyProfileService _companyService;

    public ExpenseCategoryController(IExpenseCategoryService categoryService, ICompanyProfileService companyService)
    {
        _categoryService = categoryService;
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            return View(new List<ExpenseCategory>());
        }
        return View(await _categoryService.GetAllAsync(companies.First().Id));
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            TempData["Error"] = "Create a company before adding an expense category.";
            return RedirectToAction(nameof(Index));
        }

        var model = new ExpenseCategory { CompanyId = companies.First().Id };
        ViewBag.Companies = new SelectList(companies, "Id", "Name", model.CompanyId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExpenseCategory model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Companies = new SelectList(await _companyService.GetAllAsync(), "Id", "Name", model.CompanyId);
            return View(model);
        }

        var result = await _categoryService.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            ViewBag.Companies = new SelectList(await _companyService.GetAllAsync(), "Id", "Name", model.CompanyId);
            return View(model);
        }

        TempData["Success"] = "Expense category created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var model = await _categoryService.GetByIdAsync(id.Value);
        if (model is null)
        {
            return NotFound();
        }

        ViewBag.Companies = new SelectList(await _companyService.GetAllAsync(), "Id", "Name", model.CompanyId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ExpenseCategory model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = new SelectList(await _companyService.GetAllAsync(), "Id", "Name", model.CompanyId);
            return View(model);
        }

        var result = await _categoryService.UpdateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            ViewBag.Companies = new SelectList(await _companyService.GetAllAsync(), "Id", "Name", model.CompanyId);
            return View(model);
        }

        TempData["Success"] = "Expense category updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var model = await _categoryService.GetByIdAsync(id.Value);
        return model is null ? NotFound() : View(model);
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
            TempData["Success"] = "Expense category deleted.";
        }

        return RedirectToAction(nameof(Index));
    }
}