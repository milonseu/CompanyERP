using CompanyERP.Interfaces.Services;
using CompanyERP.ViewModels.Accounting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class StatementReportController : Controller
{
    private readonly IStatementReportService _statementService;
    private readonly ICompanyProfileService _companyService;
    private readonly ICustomerService _customerService;
    private readonly ISupplierService _supplierService;

    public StatementReportController(
        IStatementReportService statementService,
        ICompanyProfileService companyService,
        ICustomerService customerService,
        ISupplierService supplierService)
    {
        _statementService = statementService;
        _companyService = companyService;
        _customerService = customerService;
        _supplierService = supplierService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Customer(int? partyId, string? fromDate, string? toDate)
    {
        var vm = await GetStatementAsync(isCustomer: true, partyId, fromDate, toDate);
        return View("Statement", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Supplier(int? partyId, string? fromDate, string? toDate)
    {
        var vm = await GetStatementAsync(isCustomer: false, partyId, fromDate, toDate);
        return View("Statement", vm);
    }

    private async Task<StatementViewModel> GetStatementAsync(bool isCustomer, int? partyId, string? fromDate, string? toDate)
    {
        var companies = await _companyService.GetAllAsync();
        var vm = new StatementViewModel
        {
            IsCustomer = isCustomer,
            Title = isCustomer ? "Customer Statement" : "Supplier Statement",
            PartyCaption = isCustomer ? "Customer" : "Supplier"
        };

        if (companies.Count == 0)
        {
            await PopulateOptions(vm, null);
            return vm;
        }

        var companyId = companies.First().Id;
        var from = DateTime.TryParse(fromDate, out var fd) ? fd : (DateTime?)null;
        var to = DateTime.TryParse(toDate, out var td) ? td : (DateTime?)null;

        int selectedId;
        if (partyId.HasValue && partyId.Value > 0)
        {
            selectedId = partyId.Value;
            vm = isCustomer
                ? await _statementService.GetCustomerStatementAsync(companyId, selectedId, from, to)
                : await _statementService.GetSupplierStatementAsync(companyId, selectedId, from, to);
        }
        else
        {
            if (isCustomer)
            {
                var customers = (await _customerService.GetAllAsync()).Where(c => c.CompanyId == companyId).OrderBy(c => c.Name).ToList();
                var first = customers.FirstOrDefault();
                selectedId = first?.Id ?? 0;
                vm = first is not null
                    ? await _statementService.GetCustomerStatementAsync(companyId, selectedId, from, to)
                    : vm;
            }
            else
            {
                var suppliers = (await _supplierService.GetAllAsync()).Where(s => s.CompanyId == companyId).OrderBy(s => s.Name).ToList();
                var first = suppliers.FirstOrDefault();
                selectedId = first?.Id ?? 0;
                vm = first is not null
                    ? await _statementService.GetSupplierStatementAsync(companyId, selectedId, from, to)
                    : vm;
            }
        }

        vm.CompanyId = companyId;
        await PopulateOptions(vm, selectedId);
        return vm;
    }

    private async Task PopulateOptions(StatementViewModel vm, int? selectedPartyId)
    {
        var companies = await _companyService.GetAllAsync();
        var companyId = companies.Count == 0 ? 0 : companies.First().Id;
        ViewBag.Parties = vm.IsCustomer
            ? new SelectList(
                (await _customerService.GetAllAsync()).Where(c => c.CompanyId == companyId).OrderBy(c => c.Name),
                "Id", "Name", selectedPartyId)
            : new SelectList(
                (await _supplierService.GetAllAsync()).Where(s => s.CompanyId == companyId).OrderBy(s => s.Name),
                "Id", "Name", selectedPartyId);
    }
}