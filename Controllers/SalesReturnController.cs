using CompanyERP.Entities.Sales;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class SalesReturnController : Controller
{
    private readonly ISalesReturnService _returnService;
    private readonly ISalesInvoiceService _invoiceService;
    private readonly ICompanyProfileService _companyService;
    private readonly IBranchService _branchService;

    public SalesReturnController(
        ISalesReturnService returnService,
        ISalesInvoiceService invoiceService,
        ICompanyProfileService companyService,
        IBranchService branchService)
    {
        _returnService = returnService;
        _invoiceService = invoiceService;
        _companyService = companyService;
        _branchService = branchService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await _returnService.GetAllAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            TempData["Error"] = "Create a company before recording a sales return.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = companies.First().Id;
        ViewBag.Invoices = new SelectList(
            (await _invoiceService.GetAllAsync()).Where(i => i.CompanyId == companyId),
            "Id", "InvoiceNo");
        ViewBag.Today = DateTime.Today.ToString("yyyy-MM-dd");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int invoiceId, DateTime returnDate, List<SalesReturnLine> lines, string? note)
    {
        if (invoiceId <= 0)
        {
            TempData["Error"] = "Select a sales invoice to return.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _returnService.CreateFromInvoiceAsync(invoiceId, returnDate, lines ?? new List<SalesReturnLine>(), note);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Create));
        }

        TempData["Success"] = "Sales return recorded successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var salesReturn = await _returnService.GetByIdAsync(id.Value);
        return salesReturn is null ? NotFound() : View(salesReturn);
    }

    [HttpPost, ActionName("DeleteConfirmed")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _returnService.DeleteAsync(id);
        if (result.Success)
        {
            TempData["Success"] = "Sales return deleted.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetInvoiceItems(int invoiceId)
    {
        var invoice = await _invoiceService.GetByIdAsync(invoiceId);
        if (invoice is null)
        {
            return Json(new { error = "Invoice not found." });
        }

        var existing = await _returnService.GetReturnedByInvoiceAsync(invoiceId);

        var items = invoice.Lines.Select(l =>
        {
            var name = l.ItemType == SalesItemType.Product
                ? l.Product?.Name ?? $"Product #{l.ProductId}"
                : l.Service?.Name ?? $"Service #{l.ServiceId}";
            var key = (l.ItemType, l.ProductId, l.ServiceId);
            var sold = l.Quantity;
            var returned = existing.TryGetValue(key, out var r) ? r : 0;
            return new
            {
                itemType = (int)l.ItemType,
                productId = l.ProductId,
                serviceId = l.ServiceId,
                name,
                unitPrice = l.UnitPrice,
                sold,
                available = sold - returned
            };
        }).Where(x => x.available > 0).ToList();

        return Json(new { items });
    }
}