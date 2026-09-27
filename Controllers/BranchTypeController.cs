using CompanyERP.Interfaces.Services;
using CompanyERP.Services.CompanyBranch;
using CompanyERP.ViewModels.CompanyBranch;
using Microsoft.AspNetCore.Mvc;

namespace CompanyERP.Controllers;

[HasPermission("Branch.View")]
public class BranchTypeController : Controller
{
    private readonly IBranchTypeService _branchTypeService;

    public BranchTypeController(IBranchTypeService branchTypeService)
    {
        _branchTypeService = branchTypeService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var branchTypes = await _branchTypeService.GetAllAsync();
        return View(branchTypes);
    }

    [HttpGet]
    [HasPermission("Branch.Create")]
    public IActionResult Create()
    {
        return View(new BranchTypeFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Branch.Create")]
    public async Task<IActionResult> Create(BranchTypeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _branchTypeService.CreateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        TempData["Success"] = "Branch type created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [HasPermission("Branch.Edit")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var branchType = await _branchTypeService.GetByIdAsync(id.Value);
        if (branchType is null)
        {
            return NotFound();
        }

        return View(BranchTypeFormViewModel.FromEntity(branchType));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Branch.Edit")]
    public async Task<IActionResult> Edit(int id, BranchTypeFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _branchTypeService.UpdateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        TempData["Success"] = "Branch type updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var branchType = await _branchTypeService.GetByIdAsync(id.Value);
        if (branchType is null)
        {
            return NotFound();
        }

        return View(branchType);
    }

    [HttpGet]
    [HasPermission("Branch.Delete")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var branchType = await _branchTypeService.GetByIdAsync(id.Value);
        if (branchType is null)
        {
            return NotFound();
        }

        ViewBag.HasBranches = await _branchTypeService.HasBranchesAsync(id.Value);
        return View(branchType);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [HasPermission("Branch.Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _branchTypeService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Branch type deleted.";
        }

        return RedirectToAction(nameof(Index));
    }
}