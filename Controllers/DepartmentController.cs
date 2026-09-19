using CompanyERP.Interfaces.Services;
using CompanyERP.Services.Company;
using CompanyERP.Services.Employee;
using CompanyERP.ViewModels.Employee;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class DepartmentController : Controller
{
    private readonly IDepartmentService _departmentService;
    private readonly ICompanyProfileService _companyService;

    public DepartmentController(IDepartmentService departmentService, ICompanyProfileService companyService)
    {
        _departmentService = departmentService;
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var departments = await _departmentService.GetAllAsync();
        return View(departments);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!await HasCompaniesAsync())
        {
            TempData["Error"] = "Create a company before adding a department.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateOptionsAsync();
        return View(new DepartmentFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepartmentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId);
            return View(model);
        }

        var result = await _departmentService.CreateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId);
            return View(model);
        }

        TempData["Success"] = "Department created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var department = await _departmentService.GetByIdAsync(id.Value);
        if (department is null)
        {
            return NotFound();
        }

        if (!await HasCompaniesAsync())
        {
            TempData["Error"] = "Create a company before editing a department.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateOptionsAsync(department.CompanyId);
        return View(DepartmentFormViewModel.FromEntity(department));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DepartmentFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId);
            return View(model);
        }

        var result = await _departmentService.UpdateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId);
            return View(model);
        }

        TempData["Success"] = "Department updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var department = await _departmentService.GetByIdAsync(id.Value);
        if (department is null)
        {
            return NotFound();
        }

        return View(department);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var department = await _departmentService.GetByIdAsync(id.Value);
        if (department is null)
        {
            return NotFound();
        }

        ViewBag.HasEmployees = await _departmentService.HasEmployeesAsync(id.Value);
        return View(department);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _departmentService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Department deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> HasCompaniesAsync()
    {
        var companies = await _companyService.GetAllAsync();
        return companies.Count > 0;
    }

    private async Task PopulateOptionsAsync(int? companyId = null)
    {
        var companies = await _companyService.GetAllAsync();
        ViewBag.Companies = new SelectList(companies, "Id", "Name", companyId);
    }
}