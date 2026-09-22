using CompanyERP.Entities.Inventory;
using CompanyERP.Entities.Sales;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class SalesInvoiceController : Controller
{
    private readonly ISalesInvoiceService _invoiceService;
    private readonly ISalesOrderService _orderService;
    private readonly ICompanyProfileService _companyService;
    private readonly IWarehouseService _warehouseService;

    public SalesInvoiceController(
        ISalesInvoiceService invoiceService,
        ISalesOrderService orderService,
        ICompanyProfileService companyService,
        IWarehouseService warehouseService)
    {
        _invoiceService = invoiceService;
        _orderService = orderService;
        _companyService = companyService;
        _warehouseService = warehouseService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await _invoiceService.GetAllAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            TempData["Error"] = "Create a company before creating a sales invoice.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = companies.First().Id;
        ViewBag.Orders = new SelectList(await _orderService.GetConfirmedOrdersAsync(), "Id", "OrderNo");
        ViewBag.Warehouses = new SelectList(await _warehouseService.GetByCompanyIdAsync(companyId), "Id", "Name");
        ViewBag.AmountReadonly = false;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int orderId, DateTime invoiceDate, DateTime? dueDate, int warehouseId, SalesPaymentType paymentType, decimal amountPaid, string? note)
    {
        var result = await _invoiceService.CreateFromOrderAsync(orderId, invoiceDate, dueDate, warehouseId, paymentType, amountPaid, note);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            var companies = await _companyService.GetAllAsync();
            var companyId = companies.First().Id;
            ViewBag.Orders = new SelectList(await _orderService.GetConfirmedOrdersAsync(), "Id", "OrderNo", orderId);
            ViewBag.Warehouses = new SelectList(await _warehouseService.GetByCompanyIdAsync(companyId), "Id", "Name", warehouseId);
            return View();
        }

        TempData["Success"] = "Sales invoice created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetOrderInfo(int orderId)
    {
        var order = await _orderService.GetByIdAsync(orderId);
        if (order is null)
        {
            return Json(new { error = "Order not found." });
        }

        var lines = order.Lines.Select(l =>
        {
            var name = l.ItemType == SalesItemType.Product
                ? l.Product?.Name ?? $"Product #{l.ProductId}"
                : l.Service?.Name ?? $"Service #{l.ServiceId}";
            return new { name, type = l.ItemType.ToString(), qty = l.Quantity, price = l.UnitPrice, amount = l.Amount };
        }).ToList();

        return Json(new { orderNo = order.OrderNo, customerId = order.CustomerId, customerName = order.Customer?.Name, branchId = order.BranchId, total = order.Total, lines });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var invoice = await _invoiceService.GetByIdAsync(id.Value);
        return invoice is null ? NotFound() : View(invoice);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordPayment(int id, decimal amountPaid)
    {
        var result = await _invoiceService.RecordPaymentAsync(id, amountPaid);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Payment recorded.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}