using CompanyERP.Entities.Asset;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class AssetController : Controller
{
    private readonly IAssetRegisterService _assetService;
    private readonly IAssetCategoryService _categoryService;
    private readonly IAssetTypeService _typeService;
    private readonly ICompanyProfileService _companyService;
    private readonly IBranchService _branchService;
    private readonly ISupplierService _supplierService;
    private readonly IEmployeeService _employeeService;

    public AssetController(
        IAssetRegisterService assetService,
        IAssetCategoryService categoryService,
        IAssetTypeService typeService,
        ICompanyProfileService companyService,
        IBranchService branchService,
        ISupplierService supplierService,
        IEmployeeService employeeService)
    {
        _assetService = assetService;
        _categoryService = categoryService;
        _typeService = typeService;
        _companyService = companyService;
        _branchService = branchService;
        _supplierService = supplierService;
        _employeeService = employeeService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? branchId, AssetStatus? status, string? search)
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            return View(new List<AssetRegister>());
        }

        var companyId = companies.First().Id;
        var assets = await _assetService.GetAllAsync(companyId, branchId, status, search);

        ViewBag.Branches = new SelectList(
            (await _branchService.GetAllAsync()).Where(b => b.CompanyId == companyId),
            "Id", "Name", branchId);
        ViewBag.Statuses = new SelectList(Enum.GetValues<AssetStatus>(), status);
        return View(assets);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            TempData["Error"] = "Create a company before registering an asset.";
            return RedirectToAction(nameof(Index));
        }

        var model = new AssetRegister { CompanyId = companies.First().Id };
        model.AssetNo = await _assetService.GenerateAssetNoAsync(model.CompanyId, model.PurchaseDate);
        await PopulateOptionsAsync(model.CompanyId, model.BranchId, model.AssetTypeId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssetRegister asset, AssetAcquisition acquisition)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(asset.CompanyId, asset.BranchId, asset.AssetTypeId);
            return View(asset);
        }

        var result = await _assetService.CreateAsync(asset, acquisition ?? new AssetAcquisition());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(asset.CompanyId, asset.BranchId, asset.AssetTypeId);
            return View(asset);
        }

        TempData["Success"] = "Asset registered successfully.";
        return RedirectToAction(nameof(Details), new { id = asset.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var model = await _assetService.GetByIdAsync(id.Value);
        if (model is null)
        {
            return NotFound();
        }

        if (model.Status == AssetStatus.Disposed)
        {
            TempData["Error"] = "A disposed asset cannot be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await PopulateOptionsAsync(model.CompanyId, model.BranchId, model.AssetTypeId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AssetRegister asset)
    {
        if (id != asset.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(asset.CompanyId, asset.BranchId, asset.AssetTypeId);
            return View(asset);
        }

        var result = await _assetService.UpdateAsync(asset);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(asset.CompanyId, asset.BranchId, asset.AssetTypeId);
            return View(asset);
        }

        TempData["Success"] = "Asset updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var model = await _assetService.GetByIdAsync(id.Value);
        if (model is null)
        {
            return NotFound();
        }

        await PopulateOptionsAsync(model.CompanyId, model.BranchId, model.AssetTypeId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var model = await _assetService.GetByIdAsync(id.Value);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _assetService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Asset deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(AssetAssignment assignment)
    {
        var result = await _assetService.AssignAsync(assignment);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Asset assigned to employee.";
        }

        return RedirectToAction(nameof(Details), new { id = assignment.AssetRegisterId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(int assetId, DateTime returnedDate)
    {
        var result = await _assetService.ReturnAssetAsync(assetId, returnedDate);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Asset returned.";
        }

        return RedirectToAction(nameof(Details), new { id = assetId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AttachDocument(AssetDocument document)
    {
        var result = await _assetService.AddDocumentAsync(document);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Document attached.";
        }

        return RedirectToAction(nameof(Details), new { id = document.AssetRegisterId });
    }

    private async Task PopulateOptionsAsync(int companyId, int selectedBranchId, int selectedTypeId)
    {
        var companies = await _companyService.GetAllAsync();
        ViewBag.Companies = new SelectList(companies, "Id", "Name", companyId);

        var branches = (await _branchService.GetAllAsync()).Where(b => b.CompanyId == companyId);
        ViewBag.Branches = new SelectList(branches, "Id", "Name", selectedBranchId);

        var categories = await _categoryService.GetAllAsync(companyId);
        ViewBag.Categories = new SelectList(categories, "Id", "Name");
        ViewBag.CategoriesJson = System.Text.Json.JsonSerializer.Serialize(categories.Select(c => new { c.Id, c.Name }));

        var types = await _typeService.GetAllAsync(companyId);
        ViewBag.Types = new SelectList(types, "Id", "Name", selectedTypeId);
        ViewBag.TypesJson = System.Text.Json.JsonSerializer.Serialize(types.Select(t => new { t.Id, t.Name, t.AssetCategoryId }));

        var suppliers = (await _supplierService.GetAllAsync()).Where(s => s.CompanyId == companyId);
        ViewBag.Suppliers = new SelectList(suppliers, "Id", "Name");

        var employees = (await _employeeService.GetAllAsync()).Where(e => e.CompanyId == companyId);
        ViewBag.Employees = new SelectList(employees, "Id", "Name");
        ViewBag.EmployeesJson = System.Text.Json.JsonSerializer.Serialize(employees.Select(e => new { e.Id, e.Name }));
    }
}