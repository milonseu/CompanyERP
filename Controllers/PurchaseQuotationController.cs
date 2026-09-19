using CompanyERP.Entities.CompanyBranch;
using CompanyERP.Entities.Inventory;
using CompanyERP.Entities.Purchase;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class PurchaseQuotationController : Controller
{
    private readonly IPurchaseQuotationService _quotationService;
    private readonly IPurchaseRequestService _requestService;
    private readonly ICompanyProfileService _companyService;
    private readonly ISupplierService _supplierService;
    private readonly IProductService _productService;

    public PurchaseQuotationController(
        IPurchaseQuotationService quotationService,
        IPurchaseRequestService requestService,
        ICompanyProfileService companyService,
        ISupplierService supplierService,
        IProductService productService)
    {
        _quotationService = quotationService;
        _requestService = requestService;
        _companyService = companyService;
        _supplierService = supplierService;
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await _quotationService.GetAllAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            TempData["Error"] = "Create a company before adding a purchase quotation.";
            return RedirectToAction(nameof(Index));
        }

        var model = new PurchaseQuotation
        {
            CompanyId = companies.First().Id,
            QuotationDate = DateTime.Today,
            ValidUntil = DateTime.Today.AddDays(7),
            Status = PurchaseQuotationStatus.Draft
        };
        model.QuotationNo = await _quotationService.GenerateNumberAsync(model.CompanyId, model.QuotationDate);
        await PopulateOptionsAsync(model.CompanyId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseQuotation model, List<PurchaseQuotationLine> lines)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId);
            return View(model);
        }

        var result = await _quotationService.CreateAsync(model, lines ?? new List<PurchaseQuotationLine>());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId);
            return View(model);
        }

        TempData["Success"] = "Purchase quotation created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var quotation = await _quotationService.GetByIdAsync(id.Value);
        if (quotation is null)
        {
            return NotFound();
        }

        if (quotation.Status != PurchaseQuotationStatus.Draft)
        {
            TempData["Error"] = "Only draft quotations can be edited.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateOptionsAsync(quotation.CompanyId);
        return View(quotation);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PurchaseQuotation model, List<PurchaseQuotationLine> lines)
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

        var result = await _quotationService.UpdateAsync(model, lines ?? new List<PurchaseQuotationLine>());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId);
            return View(model);
        }

        TempData["Success"] = "Purchase quotation updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var quotation = await _quotationService.GetByIdAsync(id.Value);
        if (quotation is null)
        {
            return NotFound();
        }

        return View(quotation);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var quotation = await _quotationService.GetByIdAsync(id.Value);
        if (quotation is null)
        {
            return NotFound();
        }

        return View(quotation);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _quotationService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Purchase quotation deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendToSupplier(int id)
    {
        var result = await _quotationService.UpdateStatusAsync(id, PurchaseQuotationStatus.Sent);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Quotation sent to supplier.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _quotationService.UpdateStatusAsync(id, PurchaseQuotationStatus.Cancelled);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Quotation cancelled.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsAsync(int companyId)
    {
        var companies = await _companyService.GetAllAsync();
        ViewBag.Companies = new SelectList(companies, "Id", "Name", companyId);

        var suppliers = (await _supplierService.GetAllAsync())
            .Where(s => s.CompanyId == companyId);
        ViewBag.Suppliers = new SelectList(suppliers, "Id", "Name");

        var requests = (await _requestService.GetAllAsync())
            .Where(r => r.CompanyId == companyId && r.Status != PurchaseRequestStatus.Cancelled);
        ViewBag.Requests = new SelectList(requests, "Id", "RequestNo");

        var products = (await _productService.GetAllAsync())
            .Where(p => p.CompanyId == companyId);
        ViewBag.Products = new SelectList(products, "Id", "Name");
        ViewBag.ProductsJson = System.Text.Json.JsonSerializer.Serialize(
            products.Select(p => new { p.Id, p.Name }).ToList());
    }
}