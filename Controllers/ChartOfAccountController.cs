using CompanyERP.Entities.Accounting;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class ChartOfAccountController : Controller
{
    private readonly IChartOfAccountService _accountService;
    private readonly ICompanyProfileService _companyService;

    public ChartOfAccountController(IChartOfAccountService accountService, ICompanyProfileService companyService)
    {
        _accountService = accountService;
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            return View(new List<ChartOfAccount>());
        }

        var accounts = await _accountService.GetAllAsync(companies.First().Id);
        ViewBag.Companies = new SelectList(companies, "Id", "Name");
        return View(accounts);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Companies = new SelectList(companies, "Id", "Name");
        return View(new ChartOfAccount { CompanyId = companies.First().Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ChartOfAccount model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCompaniesAsync(model.CompanyId);
            return View(model);
        }

        var result = await _accountService.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateCompaniesAsync(model.CompanyId);
            return View(model);
        }

        TempData["Success"] = "Account created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var account = await _accountService.GetByIdAsync(id.Value);
        if (account is null)
        {
            return NotFound();
        }

        ViewBag.Companies = new SelectList(await _companyService.GetAllAsync(), "Id", "Name", account.CompanyId);
        return View(account);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ChartOfAccount model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateCompaniesAsync(model.CompanyId);
            return View(model);
        }

        var result = await _accountService.UpdateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateCompaniesAsync(model.CompanyId);
            return View(model);
        }

        TempData["Success"] = "Account updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var account = await _accountService.GetByIdAsync(id.Value);
        if (account is null)
        {
            return NotFound();
        }

        return View(account);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _accountService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Account deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCompaniesAsync(int? selectedId = null)
    {
        ViewBag.Companies = new SelectList(await _companyService.GetAllAsync(), "Id", "Name", selectedId);
    }
}