using CompanyERP.Entities.Purchase;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class PurchaseReceivingController : Controller
{
    private readonly IPurchaseReceivingService _receivingService;
    private readonly IPurchaseOrderService _orderService;

    public PurchaseReceivingController(IPurchaseReceivingService receivingService, IPurchaseOrderService orderService)
    {
        _receivingService = receivingService;
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await _receivingService.GetAllAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var orders = await _orderService.GetReceivableOrdersAsync();
        if (orders.Count == 0)
        {
            TempData["Error"] = "No approved purchase orders are available to receive. Create and approve a purchase order first.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Orders = new SelectList(orders, "Id", "OrderNo");
        ViewBag.OrderJson = System.Text.Json.JsonSerializer.Serialize(
            orders.Select(o => new
            {
                o.Id,
                o.OrderNo,
                o.CompanyId,
                o.WarehouseId,
                WarehouseName = o.Warehouse != null ? o.Warehouse.Name : "N/A",
                Lines = o.Lines.Select(l => new
                {
                    l.ProductId,
                    ProductName = l.Product != null ? l.Product.Name : "N/A",
                    l.Quantity,
                    l.UnitCost
                }).ToList()
            }).ToList());

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int orderId, DateTime receivedDate, string? referenceNo, string? note)
    {
        if (orderId <= 0)
        {
            TempData["Error"] = "Select a purchase order to receive.";
            return RedirectToAction(nameof(Create));
        }

        var result = await _receivingService.ReceiveAsync(orderId, receivedDate, referenceNo, note);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Create));
        }

        TempData["Success"] = "Goods received, inventory updated, and purchase invoice created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var receiving = await _receivingService.GetByIdAsync(id.Value);
        if (receiving is null)
        {
            return NotFound();
        }

        return View(receiving);
    }
}