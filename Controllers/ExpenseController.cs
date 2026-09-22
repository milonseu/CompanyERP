using CompanyERP.Entities.Expense;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class ExpenseController : Controller
{
    private readonly IExpenseEntryService _entryService;
    private readonly IExpenseCategoryService _categoryService;
    private readonly IExpenseTypeService _typeService;
    private readonly ICompanyProfileService _companyService;
    private readonly IBranchService _branchService;
    private readonly ISupplierService _supplierService;

    public ExpenseController(
        IExpenseEntryService entryService,
        IExpenseCategoryService categoryService,
        IExpenseTypeService typeService,
        ICompanyProfileService companyService,
        IBranchService branchService,
        ISupplierService supplierService)
    {
        _entryService = entryService;
        _categoryService = categoryService;
        _typeService = typeService;
        _companyService = companyService;
        _branchService = branchService;
        _supplierService = supplierService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? branchId, int? typeId)
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            return View(new List<ExpenseEntry>());
        }

        var companyId = companies.First().Id;
        ViewBag.Branches = new SelectList(
            (await _branchService.GetAllAsync()).Where(b => b.CompanyId == companyId),
            "Id", "Name", branchId);
        ViewBag.Types = new SelectList(await _typeService.GetAllAsync(companyId), "Id", "Name", typeId);
        return View(await _entryService.GetAllAsync(companyId, branchId, typeId));
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            TempData["Error"] = "Create a company before adding an expense entry.";
            return RedirectToAction(nameof(Index));
        }

        var model = new ExpenseEntry { CompanyId = companies.First().Id };
        model.ExpenseNo = await _entryService.GenerateExpenseNoAsync(model.CompanyId, model.ExpenseDate);
        await PopulateOptionsAsync(model.CompanyId, model.BranchId, model.ExpenseTypeId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExpenseEntry model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId, model.BranchId, model.ExpenseTypeId);
            return View(model);
        }

        var result = await _entryService.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId, model.BranchId, model.ExpenseTypeId);
            return View(model);
        }

        TempData["Success"] = "Expense entry created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var model = await _entryService.GetByIdAsync(id.Value);
        if (model is null)
        {
            return NotFound();
        }

        await PopulateOptionsAsync(model.CompanyId, model.BranchId, model.ExpenseTypeId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ExpenseEntry model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId, model.BranchId, model.ExpenseTypeId);
            return View(model);
        }

        var result = await _entryService.UpdateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId, model.BranchId, model.ExpenseTypeId);
            return View(model);
        }

        TempData["Success"] = "Expense entry updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var model = await _entryService.GetByIdAsync(id.Value);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _entryService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Expense entry deleted.";
        }

        return RedirectToAction(nameof(Index));
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
        ViewBag.TypesJson = System.Text.Json.JsonSerializer.Serialize(types.Select(t => new { t.Id, t.Name, t.ExpenseCategoryId }));

        var suppliers = (await _supplierService.GetAllAsync()).Where(s => s.CompanyId == companyId);
        ViewBag.Suppliers = new SelectList(suppliers, "Id", "Name");
    }
}