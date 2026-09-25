using CompanyERP.Entities.Asset;
using CompanyERP.Entities.Customer;
using CompanyERP.Entities.Expense;
using CompanyERP.Entities.Payment;
using CompanyERP.Entities.Supplier;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class PaymentController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly IPaymentMethodService _methodService;
    private readonly ICashAccountService _cashAccountService;
    private readonly IBankAccountService _bankAccountService;
    private readonly ICompanyProfileService _companyService;
    private readonly IBranchService _branchService;
    private readonly ICustomerService _customerService;
    private readonly ISupplierService _supplierService;
    private readonly IExpenseEntryService _expenseService;
    private readonly IAssetRegisterService _assetService;

    public PaymentController(
        IPaymentService paymentService,
        IPaymentMethodService methodService,
        ICashAccountService cashAccountService,
        IBankAccountService bankAccountService,
        ICompanyProfileService companyService,
        IBranchService branchService,
        ICustomerService customerService,
        ISupplierService supplierService,
        IExpenseEntryService expenseService,
        IAssetRegisterService assetService)
    {
        _paymentService = paymentService;
        _methodService = methodService;
        _cashAccountService = cashAccountService;
        _bankAccountService = bankAccountService;
        _companyService = companyService;
        _branchService = branchService;
        _customerService = customerService;
        _supplierService = supplierService;
        _expenseService = expenseService;
        _assetService = assetService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(PaymentCategory? category)
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            return View(new List<Payment>());
        }

        var companyId = companies.First().Id;
        ViewBag.Categories = new SelectList(Enum.GetValues<PaymentCategory>(), category);
        return View(await _paymentService.GetAllAsync(companyId, category));
    }

    [HttpGet]
    public async Task<IActionResult> Create(PaymentCategory? category)
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            TempData["Error"] = "Create a company before recording a payment.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = companies.First().Id;
        var model = new Payment { CompanyId = companyId };
        if (category.HasValue)
        {
            model.Category = category.Value;
        }

        model.PaymentNo = await _paymentService.GeneratePaymentNoAsync(companyId, model.PaymentDate);
        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Payment model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var result = await _paymentService.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model);
            return View(model);
        }

        TempData["Success"] = "Payment recorded successfully.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var model = await _paymentService.GetByIdAsync(id.Value);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _paymentService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Payment deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsAsync(Payment model)
    {
        var companies = await _companyService.GetAllAsync();
        ViewBag.Companies = new SelectList(companies, "Id", "Name", model.CompanyId);

        var branches = (await _branchService.GetAllAsync()).Where(b => b.CompanyId == model.CompanyId);
        ViewBag.Branches = new SelectList(branches, "Id", "Name", model.BranchId);

        ViewBag.Categories = new SelectList(Enum.GetValues<PaymentCategory>(), model.Category);

        ViewBag.PaymentMethods = new SelectList(
            await _methodService.GetAllAsync(model.CompanyId), "Id", "Name", model.PaymentMethodId == 0 ? null : model.PaymentMethodId);

        ViewBag.CashAccounts = new SelectList(
            await _cashAccountService.GetAllAsync(model.CompanyId), "Id", "AccountName", model.CashAccountId);
        ViewBag.BankAccounts = new SelectList(
            await _bankAccountService.GetAllAsync(model.CompanyId), "Id", "AccountName", model.BankAccountId);

        var customers = (await _customerService.GetAllAsync()).Where(c => c.CompanyId == model.CompanyId);
        ViewBag.Customers = new SelectList(customers, "Id", "Name", model.CustomerId);

        var suppliers = (await _supplierService.GetAllAsync()).Where(s => s.CompanyId == model.CompanyId);
        ViewBag.Suppliers = new SelectList(suppliers, "Id", "Name", model.SupplierId);

        var expenses = new List<ExpenseEntry>();
        foreach (var entry in await _expenseService.GetAllAsync(model.CompanyId))
        {
            if (entry.Amount > entry.AmountPaid)
            {
                expenses.Add(entry);
            }
        }

        ViewBag.ExpenseCandidates = expenses
            .Select(e => new SelectListItem(
                $"{e.ExpenseNo} - {e.Description} (due {e.Amount - e.AmountPaid:N2})",
                e.Id.ToString(),
                model.ExpenseEntryId == e.Id))
            .ToList();

        var salaryCandidates = await _paymentService.GetPendingSalaryCandidatesAsync(model.CompanyId);
        ViewBag.SalaryCandidates = salaryCandidates
            .Select(s => new SelectListItem(
                $"{s.Employee?.Name} - {s.ForMonth:yyyy-MM} ({s.Amount:N2})",
                s.Id.ToString(),
                model.SalaryPaymentId == s.Id))
            .ToList();

        var assets = new List<AssetRegister>();
        foreach (var asset in await _assetService.GetAllAsync(model.CompanyId))
        {
            var outstanding = await _paymentService.GetAssetOutstandingAsync(asset.Id);
            if (outstanding > 0)
            {
                assets.Add(asset);
            }
        }

        ViewBag.AssetCandidates = assets
            .Select(a => new SelectListItem($"{a.AssetNo} - {a.Name}", a.Id.ToString(), model.AssetRegisterId == a.Id))
            .ToList();
    }
}