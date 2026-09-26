using CompanyERP.Interfaces.Services;
using CompanyERP.ViewModels.Accounting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class AgingReportController : Controller
{
    private readonly IAgingReportService _agingService;
    private readonly ICompanyProfileService _companyService;
    private readonly IBranchService _branchService;
    private readonly ICustomerService _customerService;
    private readonly ISupplierService _supplierService;

    public AgingReportController(
        IAgingReportService agingService,
        ICompanyProfileService companyService,
        IBranchService branchService,
        ICustomerService customerService,
        ISupplierService supplierService)
    {
        _agingService = agingService;
        _companyService = companyService;
        _branchService = branchService;
        _customerService = customerService;
        _supplierService = supplierService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Receivables(string? asOfDate, int? branchId, int? partyId)
    {
        var vm = await GetReportAsync(isReceivables: true, asOfDate, branchId, partyId);
        return View("Report", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Payables(string? asOfDate, int? branchId, int? partyId)
    {
        var vm = await GetReportAsync(isReceivables: false, asOfDate, branchId, partyId);
        return View("Report", vm);
    }

    private async Task<AgingReportViewModel> GetReportAsync(bool isReceivables, string? asOfDate, int? branchId, int? partyId)
    {
        var companies = await _companyService.GetAllAsync();
        var vm = new AgingReportViewModel
        {
            IsReceivables = isReceivables,
            Title = isReceivables ? "Receivable Aging" : "Payable Aging",
            PartyCaption = isReceivables ? "Customer" : "Supplier"
        };

        if (companies.Count == 0)
        {
            return vm;
        }

        var companyId = companies.First().Id;
        var date = DateTime.TryParse(asOfDate, out var parsed) ? parsed : DateTime.Today;

        vm = isReceivables
            ? await _agingService.GetReceivablesAsync(companyId, date, branchId, partyId)
            : await _agingService.GetPayablesAsync(companyId, date, branchId, partyId);
        vm.CompanyId = companyId;

        await PopulateOptions(vm);
        return vm;
    }

    private async Task PopulateOptions(AgingReportViewModel vm)
    {
        var branches = (await _branchService.GetAllAsync()).Where(b => b.CompanyId == vm.CompanyId);
        ViewBag.Branches = new SelectList(branches, "Id", "Name", vm.BranchId);

        ViewBag.Parties = vm.IsReceivables
            ? new SelectList(
                (await _customerService.GetAllAsync()).Where(c => c.CompanyId == vm.CompanyId).OrderBy(c => c.Name),
                "Id", "Name", vm.PartyId)
            : new SelectList(
                (await _supplierService.GetAllAsync()).Where(s => s.CompanyId == vm.CompanyId).OrderBy(s => s.Name),
                "Id", "Name", vm.PartyId);
    }
}