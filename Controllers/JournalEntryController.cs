using CompanyERP.Interfaces.Services;
using CompanyERP.ViewModels.Accounting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class JournalEntryController : Controller
{
    private readonly IJournalEntryService _journalEntryService;
    private readonly IChartOfAccountService _accountService;
    private readonly ICompanyProfileService _companyService;
    private readonly IBranchService _branchService;

    public JournalEntryController(
        IJournalEntryService journalEntryService,
        IChartOfAccountService accountService,
        ICompanyProfileService companyService,
        IBranchService branchService)
    {
        _journalEntryService = journalEntryService;
        _accountService = accountService;
        _companyService = companyService;
        _branchService = branchService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int companyId = 0, int? branchId = null)
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            return View(new List<CompanyERP.Entities.Accounting.JournalEntry>());
        }

        if (companyId <= 0)
        {
            companyId = companies.First().Id;
        }

        var entries = await _journalEntryService.GetAllAsync(companyId, branchId: branchId);
        ViewBag.Companies = new SelectList(companies, "Id", "Name", companyId);
        ViewBag.Branches = new SelectList(
            (await _branchService.GetAllAsync()).Where(b => b.CompanyId == companyId),
            "Id", "Name", branchId);
        ViewBag.SelectedCompanyId = companyId;
        return View(entries);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var entry = await _journalEntryService.GetByIdAsync(id.Value);
        if (entry is null)
        {
            return NotFound();
        }

        return View(entry);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            return RedirectToAction(nameof(Index));
        }

        var companyId = companies.First().Id;
        var model = new JournalEntryFormViewModel
        {
            CompanyId = companyId,
            EntryNo = await _journalEntryService.GenerateEntryNoAsync(companyId, DateTime.Today)
        };

        await PopulateFormAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(JournalEntryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateFormAsync(model);
            return View(model);
        }

        var entry = new CompanyERP.Entities.Accounting.JournalEntry
        {
            CompanyId = model.CompanyId,
            EntryDate = model.EntryDate,
            EntryNo = model.EntryNo,
            Description = model.Description
        };

        var details = model.Lines
            .Where(l => l.Debit > 0 || l.Credit > 0)
            .Select(l => new CompanyERP.Entities.Accounting.JournalEntryDetail
            {
                AccountId = l.AccountId,
                Debit = l.Debit,
                Credit = l.Credit,
                Note = l.Note
            })
            .ToList();

        var result = await _journalEntryService.CreateAsync(entry, details);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateFormAsync(model);
            return View(model);
        }

        // Reload by reference: the journal was created through the posting service.
        var created = await _journalEntryService.GetAllAsync(model.CompanyId);
        var latest = created.FirstOrDefault(e =>
            e.SourceModule == "Manual Journal" &&
            e.Description.Trim() == model.Description.Trim() &&
            e.EntryDate.Date == model.EntryDate.Date);

        TempData["Success"] = $"Journal entry {latest?.EntryNo} posted successfully.";
        return RedirectToAction(nameof(Details), new { id = latest?.Id ?? 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _journalEntryService.DeleteAsync(id);
        TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateFormAsync(JournalEntryFormViewModel model)
    {
        ViewBag.Companies = new SelectList(await _companyService.GetAllAsync(), "Id", "Name", model.CompanyId);
        var accounts = await _accountService.GetPostableAsync(model.CompanyId);
        ViewBag.Accounts = new SelectList(accounts, "Id", "AccountDisplayLabel");
    }
}