using CompanyERP.Interfaces.Services;
using CompanyERP.Services.Company;
using CompanyERP.ViewModels.Company;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class FinancialYearController : Controller
{
    private readonly IFinancialYearService _financialYearService;
    private readonly IAccountingPeriodService _accountingPeriodService;

    public FinancialYearController(
        IFinancialYearService financialYearService,
        IAccountingPeriodService accountingPeriodService)
    {
        _financialYearService = financialYearService;
        _accountingPeriodService = accountingPeriodService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var years = await _financialYearService.GetAllAsync();
        return View(years);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new FinancialYearFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FinancialYearFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _financialYearService.CreateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        TempData["Success"] = "Financial year created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var year = await _financialYearService.GetByIdAsync(id.Value);
        if (year is null)
        {
            return NotFound();
        }

        return View(FinancialYearFormViewModel.FromEntity(year));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FinancialYearFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _financialYearService.UpdateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        TempData["Success"] = "Financial year updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var year = await _financialYearService.GetByIdAsync(id.Value);
        if (year is null)
        {
            return NotFound();
        }

        return View(year);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var year = await _financialYearService.GetByIdAsync(id.Value);
        if (year is null)
        {
            return NotFound();
        }

        return View(year);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _financialYearService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Financial year deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Periods(int id)
    {
        var periods = await _accountingPeriodService.GetAllAsync(id);
        ViewData["FinancialYearId"] = id;
        return View(periods);
    }
}