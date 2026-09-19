using CompanyERP.Interfaces.Services;
using CompanyERP.Services.Company;
using CompanyERP.Services.CompanyBranch;
using CompanyERP.Services.Employee;
using CompanyERP.ViewModels.Employee;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class EmployeeController : Controller
{
    private readonly IEmployeeService _employeeService;
    private readonly ICompanyProfileService _companyService;
    private readonly IDepartmentService _departmentService;
    private readonly IDesignationService _designationService;
    private readonly IBranchService _branchService;

    public EmployeeController(
        IEmployeeService employeeService,
        ICompanyProfileService companyService,
        IDepartmentService departmentService,
        IDesignationService designationService,
        IBranchService branchService)
    {
        _employeeService = employeeService;
        _companyService = companyService;
        _departmentService = departmentService;
        _designationService = designationService;
        _branchService = branchService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var employees = await _employeeService.GetAllAsync();
        return View(employees);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            TempData["Error"] = "Create a company before adding an employee.";
            return RedirectToAction(nameof(Index));
        }

        var branches = await _branchService.GetAllAsync();
        if (branches.Count == 0)
        {
            TempData["Error"] = "Create a branch before adding an employee.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateOptionsAsync();
        return View(new EmployeeFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId, model.DepartmentId, model.DesignationId, model.DefaultBranchId);
            return View(model);
        }

        var result = await _employeeService.CreateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId, model.DepartmentId, model.DesignationId, model.DefaultBranchId);
            return View(model);
        }

        TempData["Success"] = "Employee created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var employee = await _employeeService.GetByIdAsync(id.Value);
        if (employee is null)
        {
            return NotFound();
        }

        await PopulateOptionsAsync(employee.CompanyId, employee.DepartmentId, employee.DesignationId, employee.DefaultBranchId);
        return View(EmployeeFormViewModel.FromEntity(employee));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmployeeFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId, model.DepartmentId, model.DesignationId, model.DefaultBranchId);
            return View(model);
        }

        var result = await _employeeService.UpdateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId, model.DepartmentId, model.DesignationId, model.DefaultBranchId);
            return View(model);
        }

        TempData["Success"] = "Employee updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var employee = await _employeeService.GetByIdAsync(id.Value);
        if (employee is null)
        {
            return NotFound();
        }

        return View(employee);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var employee = await _employeeService.GetByIdAsync(id.Value);
        if (employee is null)
        {
            return NotFound();
        }

        ViewBag.HasPayments = employee.SalaryPayments.Count > 0;
        return View(employee);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _employeeService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Employee deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    // ---- Salary Structure ----

    [HttpGet]
    public async Task<IActionResult> SalaryStructure(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var employee = await _employeeService.GetByIdAsync(id.Value);
        if (employee is null)
        {
            return NotFound();
        }

        ViewBag.EmployeeName = employee.Name;
        var structure = await _employeeService.GetSalaryStructureAsync(id.Value);
        return View(structure is not null
            ? SalaryStructureFormViewModel.FromEntity(structure)
            : new SalaryStructureFormViewModel { EmployeeId = id.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalaryStructure(int employeeId, SalaryStructureFormViewModel model)
    {
        if (employeeId != model.EmployeeId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var employee = await _employeeService.GetByIdAsync(employeeId);
            ViewBag.EmployeeName = employee?.Name ?? string.Empty;
            return View(model);
        }

        var result = await _employeeService.SaveSalaryStructureAsync(model.ToEntity());
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Salary structure saved successfully.";
        }

        return RedirectToAction(nameof(Details), new { id = employeeId });
    }

    // ---- Branch Assignments ----

    [HttpGet]
    public async Task<IActionResult> BranchAssignments(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var employee = await _employeeService.GetByIdAsync(id.Value);
        if (employee is null)
        {
            return NotFound();
        }

        ViewBag.EmployeeName = employee.Name;
        ViewBag.Assignments = await _employeeService.GetBranchAssignmentsAsync(id.Value);
        await PopulateBranchOptionsAsync();
        return View(new EmployeeBranchAssignmentFormViewModel { EmployeeId = id.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBranchAssignment(EmployeeBranchAssignmentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var employee = await _employeeService.GetByIdAsync(model.EmployeeId);
            ViewBag.EmployeeName = employee?.Name ?? string.Empty;
            await PopulateBranchOptionsAsync(model.BranchId);
            return View(nameof(BranchAssignments), model);
        }

        var result = await _employeeService.AddBranchAssignmentAsync(model.ToEntity());
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Branch assignment added successfully.";
        }

        return RedirectToAction(nameof(BranchAssignments), new { id = model.EmployeeId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBranchAssignment(int id, int employeeId)
    {
        var result = await _employeeService.RemoveBranchAssignmentAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Branch assignment removed.";
        }

        return RedirectToAction(nameof(BranchAssignments), new { id = employeeId });
    }

    // ---- Salary Payments ----

    [HttpGet]
    public async Task<IActionResult> SalaryPayments(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var employee = await _employeeService.GetByIdAsync(id.Value);
        if (employee is null)
        {
            return NotFound();
        }

        ViewBag.EmployeeName = employee.Name;
        ViewBag.Payments = await _employeeService.GetSalaryPaymentsAsync(id.Value);
        return View(new SalaryPaymentFormViewModel { EmployeeId = id.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSalaryPayment(SalaryPaymentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var employee = await _employeeService.GetByIdAsync(model.EmployeeId);
            ViewBag.EmployeeName = employee?.Name ?? string.Empty;
            return View(nameof(SalaryPayments), model);
        }

        var result = await _employeeService.CreateSalaryPaymentAsync(model.ToEntity());
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Salary payment recorded successfully.";
        }

        return RedirectToAction(nameof(SalaryPayments), new { id = model.EmployeeId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSalaryPayment(int id, int employeeId)
    {
        var result = await _employeeService.DeleteSalaryPaymentAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Salary payment deleted.";
        }

        return RedirectToAction(nameof(SalaryPayments), new { id = employeeId });
    }

    // ---- Asset Assignments ----

    [HttpGet]
    public async Task<IActionResult> AssetAssignments(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var employee = await _employeeService.GetByIdAsync(id.Value);
        if (employee is null)
        {
            return NotFound();
        }

        ViewBag.EmployeeName = employee.Name;
        ViewBag.Assets = await _employeeService.GetAssetAssignmentsAsync(id.Value);
        return View(new EmployeeAssetAssignmentFormViewModel { EmployeeId = id.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAssetAssignment(EmployeeAssetAssignmentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var employee = await _employeeService.GetByIdAsync(model.EmployeeId);
            ViewBag.EmployeeName = employee?.Name ?? string.Empty;
            return View(nameof(AssetAssignments), model);
        }

        var result = await _employeeService.AddAssetAssignmentAsync(model.ToEntity());
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Asset assigned successfully.";
        }

        return RedirectToAction(nameof(AssetAssignments), new { id = model.EmployeeId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAssetReturned(int id, int employeeId)
    {
        var result = await _employeeService.MarkAssetReturnedAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Asset marked as returned.";
        }

        return RedirectToAction(nameof(AssetAssignments), new { id = employeeId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAssetAssignment(int id, int employeeId)
    {
        var result = await _employeeService.RemoveAssetAssignmentAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Asset assignment removed.";
        }

        return RedirectToAction(nameof(AssetAssignments), new { id = employeeId });
    }

    private async Task PopulateOptionsAsync(int? companyId = null, int? departmentId = null, int? designationId = null, int? branchId = null)
    {
        var companies = await _companyService.GetAllAsync();
        var branches = await _branchService.GetAllAsync();
        ViewBag.Companies = new SelectList(companies, "Id", "Name", companyId);
        ViewBag.Departments = companyId.HasValue
            ? await _departmentService.GetByCompanyIdAsync(companyId.Value)
            : await _departmentService.GetAllAsync();
        ViewBag.Designations = companyId.HasValue
            ? await _designationService.GetByCompanyIdAsync(companyId.Value)
            : await _designationService.GetAllAsync();
        ViewBag.Branches = new SelectList(branches, "Id", "Name", branchId);
    }

    private async Task PopulateBranchOptionsAsync(int? branchId = null)
    {
        var branches = await _branchService.GetAllAsync();
        ViewBag.Branches = new SelectList(branches, "Id", "Name", branchId);
    }
}