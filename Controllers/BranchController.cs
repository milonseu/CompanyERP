using CompanyERP.Interfaces.Services;
using CompanyERP.Services.Company;
using CompanyERP.Services.CompanyBranch;
using CompanyERP.ViewModels.CompanyBranch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class BranchController : Controller
{
    private readonly IBranchService _branchService;
    private readonly IBranchTypeService _branchTypeService;
    private readonly ICompanyProfileService _companyService;

    public BranchController(
        IBranchService branchService,
        IBranchTypeService branchTypeService,
        ICompanyProfileService companyService)
    {
        _branchService = branchService;
        _branchTypeService = branchTypeService;
        _companyService = companyService;
    }

[HttpGet]
    public async Task<IActionResult> Index()
    {
        var branches = await _branchService.GetAllAsync();
        var usage = await _branchService.GetUsageCountsAsync();

        var viewModel = branches.Select(b =>
        {
            var u = usage.TryGetValue(b.Id, out var counts) ? counts : (Employees: 0, Warehouses: 0);
            return new BranchIndexViewModel
            {
                Branch = b,
                EmployeeCount = u.Employees,
                WarehouseCount = u.Warehouses
            };
        }).ToList();

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var companies = await _companyService.GetAllAsync();
        var branchTypes = await _branchTypeService.GetAllAsync();

        if (companies.Count == 0)
        {
            TempData["Error"] = "Create a company before adding a branch.";
            return RedirectToAction(nameof(Index));
        }
        if (branchTypes.Count == 0)
        {
            TempData["Error"] = "Create a branch type before adding a branch.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateOptionsAsync();
        return View(new BranchFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BranchFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId, model.BranchTypeId);
            return View(model);
        }

        var result = await _branchService.CreateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId, model.BranchTypeId);
            return View(model);
        }

        TempData["Success"] = "Branch created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var branch = await _branchService.GetByIdAsync(id.Value);
        if (branch is null)
        {
            return NotFound();
        }

        await PopulateOptionsAsync(branch.CompanyId, branch.BranchTypeId);
        return View(BranchFormViewModel.FromEntity(branch));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BranchFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId, model.BranchTypeId);
            return View(model);
        }

        var result = await _branchService.UpdateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId, model.BranchTypeId);
            return View(model);
        }

        TempData["Success"] = "Branch updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var branch = await _branchService.GetByIdAsync(id.Value);
        if (branch is null)
        {
            return NotFound();
        }

        return View(branch);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var branch = await _branchService.GetByIdAsync(id.Value);
        if (branch is null)
        {
            return NotFound();
        }

        return View(branch);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _branchService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Branch deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Settings(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var branch = await _branchService.GetByIdAsync(id.Value);
        if (branch is null)
        {
            return NotFound();
        }

        ViewBag.BranchName = branch.Name;
        return View(BranchSettingsFormViewModel.FromEntity(branch.Settings!));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(int id, BranchSettingsFormViewModel model)
    {
        if (id != model.BranchId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var branch = await _branchService.GetByIdAsync(id);
            ViewBag.BranchName = branch?.Name ?? string.Empty;
            return View(model);
        }

        var result = await _branchService.UpdateSettingsAsync(model.ToEntity());
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Branch settings updated successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsAsync(int? companyId = null, int? branchTypeId = null)
    {
        var companies = await _companyService.GetAllAsync();
        var branchTypes = await _branchTypeService.GetAllAsync();
        ViewBag.Companies = new SelectList(companies, "Id", "Name", companyId);
        ViewBag.BranchTypes = new SelectList(branchTypes, "Id", "Name", branchTypeId);
    }
}