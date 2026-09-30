using CompanyERP.Entities.Accounting;
using CompanyERP.Interfaces.Services;
using CompanyERP.ViewModels.Accounting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

[HasPermission("Accounting.View")]
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
            return View(new ChartOfAccountIndexViewModel());
        }

        var tree = await _accountService.GetTreeAsync(companies.First().Id);
        ViewBag.Companies = new SelectList(companies, "Id", "Name");
        return View(new ChartOfAccountIndexViewModel().Build(tree));
    }

    [HttpGet]
    [HasPermission("Accounting.Create")]
    public async Task<IActionResult> Create(int? parentId)
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            return RedirectToAction(nameof(Index));
        }

        var companyId = companies.First().Id;
        var account = new ChartOfAccount { CompanyId = companyId };

        if (parentId.HasValue)
        {
            var parent = await _accountService.GetByIdAsync(parentId.Value);
            if (parent is not null && parent.CompanyId == companyId)
            {
                account.ParentId = parent.Id;
                // Copy the parent's class and balance rather than deriving them. Deriving
                // would get contra accounts wrong (Accumulated Depreciation is an Asset
                // with a Credit balance), and the service rejects any mismatch anyway.
                account.AccountType = parent.AccountType;
                account.NormalBalance = parent.NormalBalance;
                ViewBag.ParentPath = parent.AccountDisplayLabel;
            }
        }

        await PopulateFormAsync(account);
        return View(account);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Accounting.Create")]
    public async Task<IActionResult> Create(ChartOfAccount model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateFormAsync(model);
            return View(model);
        }

        var result = await _accountService.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateFormAsync(model);
            return View(model);
        }

        TempData["Success"] = "Account created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [HasPermission("Accounting.Edit")]
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

        if (account.ParentId.HasValue)
        {
            var parent = await _accountService.GetByIdAsync(account.ParentId.Value);
            ViewBag.ParentPath = parent?.AccountDisplayLabel;
        }

        await PopulateFormAsync(account);
        return View(account);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Accounting.Edit")]
    public async Task<IActionResult> Edit(int id, ChartOfAccount model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateFormAsync(model);
            return View(model);
        }

        var result = await _accountService.UpdateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateFormAsync(model);
            return View(model);
        }

        TempData["Success"] = "Account updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [HasPermission("Accounting.Delete")]
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
    [HasPermission("Accounting.Delete")]
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

    private async Task PopulateFormAsync(ChartOfAccount model)
    {
        await PopulateCompaniesAsync(model.CompanyId);
        // Excluding self (and its descendants) is what keeps the tree acyclic; the
        // service also rejects a self parent, but the UI should not offer it.
        ViewBag.Parents = await BuildParentOptionsAsync(model.CompanyId, model.Id > 0 ? model.Id : null);
    }

    private async Task<List<ChartOfAccountParentOption>> BuildParentOptionsAsync(int companyId, int? excludeId)
    {
        var candidates = await _accountService.GetParentCandidatesAsync(companyId, excludeId);

        return candidates
            .Select(a => new ChartOfAccountParentOption
            {
                Id = a.Id,
                Label = a.AccountDisplayLabel,
                AccountType = (int)a.AccountType,
                NormalBalance = (int)a.NormalBalance
            })
            .ToList();
    }

    private async Task PopulateCompaniesAsync(int? selectedId = null)
    {
        ViewBag.Companies = new SelectList(await _companyService.GetAllAsync(), "Id", "Name", selectedId);
    }
}