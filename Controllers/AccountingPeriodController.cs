using CompanyERP.Entities.Company;
using CompanyERP.Interfaces.Services;
using CompanyERP.Services.Company;
using CompanyERP.ViewModels.Company;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class AccountingPeriodController : Controller
{
    private readonly IAccountingPeriodService _accountingPeriodService;

    public AccountingPeriodController(IAccountingPeriodService accountingPeriodService)
    {
        _accountingPeriodService = accountingPeriodService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? financialYearId)
    {
        var periods = await _accountingPeriodService.GetAllAsync(financialYearId);
        var years = await _accountingPeriodService.GetFinancialYearsAsync();
        ViewBag.FinancialYears = new SelectList(years, "Id", "Name", financialYearId);
        return View(periods);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? financialYearId)
    {
        var years = await _accountingPeriodService.GetFinancialYearsAsync();
        if (years.Count == 0)
        {
            TempData["Error"] = "Create a financial year before adding accounting periods.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.FinancialYears = new SelectList(years, "Id", "Name");
        return View(new AccountingPeriodFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccountingPeriodFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateFinancialYearsAsync(model.FinancialYearId);
            return View(model);
        }

        var result = await _accountingPeriodService.CreateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateFinancialYearsAsync(model.FinancialYearId);
            return View(model);
        }

        TempData["Success"] = "Accounting period created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var period = await _accountingPeriodService.GetByIdAsync(id.Value);
        if (period is null)
        {
            return NotFound();
        }

        await PopulateFinancialYearsAsync(period.FinancialYearId);
        return View(AccountingPeriodFormViewModel.FromEntity(period));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AccountingPeriodFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateFinancialYearsAsync(model.FinancialYearId);
            return View(model);
        }

        var result = await _accountingPeriodService.UpdateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateFinancialYearsAsync(model.FinancialYearId);
            return View(model);
        }

        TempData["Success"] = "Accounting period updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var period = await _accountingPeriodService.GetByIdAsync(id.Value);
        if (period is null)
        {
            return NotFound();
        }

        return View(period);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var period = await _accountingPeriodService.GetByIdAsync(id.Value);
        if (period is null)
        {
            return NotFound();
        }

        return View(period);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _accountingPeriodService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Accounting period deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateFinancialYearsAsync(int? selectedId = null)
    {
        var years = await _accountingPeriodService.GetFinancialYearsAsync();
        ViewBag.FinancialYears = new SelectList(years, "Id", "Name", selectedId);
    }
}