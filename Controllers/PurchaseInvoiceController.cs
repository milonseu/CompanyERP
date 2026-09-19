using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace CompanyERP.Controllers;

public class PurchaseInvoiceController : Controller
{
    private readonly IPurchaseInvoiceService _invoiceService;

    public PurchaseInvoiceController(IPurchaseInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await _invoiceService.GetAllAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var invoice = await _invoiceService.GetByIdAsync(id.Value);
        if (invoice is null)
        {
            return NotFound();
        }

        return View(invoice);
    }
}