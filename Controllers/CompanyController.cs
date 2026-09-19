using CompanyERP.Interfaces.Services;
using CompanyERP.Services.Company;
using CompanyERP.ViewModels.Company;
using Microsoft.AspNetCore.Mvc;

namespace CompanyERP.Controllers;

public class CompanyController : Controller
{
    private readonly ICompanyProfileService _companyService;

    public CompanyController(ICompanyProfileService companyService)
    {
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var companies = await _companyService.GetAllAsync();
        return View(companies);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CompanyProfileFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CompanyProfileFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _companyService.CreateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        TempData["Success"] = "Company profile created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var profile = await _companyService.GetByIdAsync(id.Value);
        if (profile is null)
        {
            return NotFound();
        }

        return View(CompanyProfileFormViewModel.FromEntity(profile));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CompanyProfileFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _companyService.UpdateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        TempData["Success"] = "Company profile updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var profile = await _companyService.GetByIdAsync(id.Value);
        if (profile is null)
        {
            return NotFound();
        }

        return View(profile);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var profile = await _companyService.GetByIdAsync(id.Value);
        if (profile is null)
        {
            return NotFound();
        }

        return View(profile);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _companyService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Company profile deleted.";
        }

        return RedirectToAction(nameof(Index));
    }
}