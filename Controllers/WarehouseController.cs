using CompanyERP.Entities.Inventory;
using CompanyERP.Interfaces.Services;
using CompanyERP.Services.Company;
using CompanyERP.Services.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class WarehouseController : Controller
{
    private readonly IWarehouseService _warehouseService;
    private readonly ICompanyProfileService _companyService;
    private readonly IBranchService _branchService;

    public WarehouseController(
        IWarehouseService warehouseService,
        ICompanyProfileService companyService,
        IBranchService branchService)
    {
        _warehouseService = warehouseService;
        _companyService = companyService;
        _branchService = branchService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var warehouses = await _warehouseService.GetAllAsync();
        return View(warehouses);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!await HasCompaniesAsync())
        {
            TempData["Error"] = "Create a company before adding a warehouse.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateOptionsAsync();
        return View(new Warehouse());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Warehouse model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId);
            return View(model);
        }

        var result = await _warehouseService.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId);
            return View(model);
        }

        TempData["Success"] = "Warehouse created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var warehouse = await _warehouseService.GetByIdAsync(id.Value);
        if (warehouse is null)
        {
            return NotFound();
        }

        if (!await HasCompaniesAsync())
        {
            TempData["Error"] = "Create a company before editing a warehouse.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateOptionsAsync(warehouse.CompanyId, warehouse.BranchId);
        return View(warehouse);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Warehouse model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId, model.BranchId);
            return View(model);
        }

        var result = await _warehouseService.UpdateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId, model.BranchId);
            return View(model);
        }

        TempData["Success"] = "Warehouse updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var warehouse = await _warehouseService.GetByIdAsync(id.Value);
        if (warehouse is null)
        {
            return NotFound();
        }

        return View(warehouse);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var warehouse = await _warehouseService.GetByIdAsync(id.Value);
        if (warehouse is null)
        {
            return NotFound();
        }

        return View(warehouse);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _warehouseService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Warehouse deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> HasCompaniesAsync()
    {
        var companies = await _companyService.GetAllAsync();
        return companies.Count > 0;
    }

    private async Task PopulateOptionsAsync(int? companyId = null, int? branchId = null)
    {
        var companies = await _companyService.GetAllAsync();
        ViewBag.Companies = new SelectList(companies, "Id", "Name", companyId);

        var allBranches = await _branchService.GetAllAsync();
        var branches = companyId.HasValue
            ? allBranches.Where(b => b.CompanyId == companyId.Value).ToList()
            : new List<CompanyERP.Entities.CompanyBranch.Branch>();
        ViewBag.Branches = new SelectList(branches, "Id", "Name", branchId);
    }
}