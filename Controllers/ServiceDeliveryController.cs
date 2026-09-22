using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class ServiceDeliveryController : Controller
{
    private readonly IServiceDeliveryService _deliveryService;
    private readonly IServiceOrderService _orderService;
    private readonly ICompanyProfileService _companyService;
    private readonly IBranchService _branchService;

    public ServiceDeliveryController(
        IServiceDeliveryService deliveryService,
        IServiceOrderService orderService,
        ICompanyProfileService companyService,
        IBranchService branchService)
    {
        _deliveryService = deliveryService;
        _orderService = orderService;
        _companyService = companyService;
        _branchService = branchService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await _deliveryService.GetAllAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            TempData["Error"] = "Create a company before recording a service delivery.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = companies.First().Id;
        ViewBag.Orders = new SelectList(
            (await _orderService.GetDeliveryEligibleAsync()).Where(o => o.CompanyId == companyId),
            "Id", "OrderNo");
        ViewBag.Branches = new SelectList(
            (await _branchService.GetAllAsync()).Where(b => b.CompanyId == companyId), "Id", "Name");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int companyId, int branchId, int serviceOrderId, DateTime deliveryDate, string? deliveredBy, decimal quantity, string? note)
    {
        var result = await _deliveryService.DeliverAsync(companyId, branchId, serviceOrderId, deliveryDate, deliveredBy, quantity, note);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            var companies = await _companyService.GetAllAsync();
            var cid = companyId == 0 ? companies.First().Id : companyId;
            ViewBag.Orders = new SelectList(
                (await _orderService.GetDeliveryEligibleAsync()).Where(o => o.CompanyId == cid),
                "Id", "OrderNo", serviceOrderId);
            ViewBag.Branches = new SelectList(
                (await _branchService.GetAllAsync()).Where(b => b.CompanyId == cid), "Id", "Name", branchId);
            return View();
        }

        TempData["Success"] = "Service delivery recorded.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var delivery = await _deliveryService.GetByIdAsync(id.Value);
        return delivery is null ? NotFound() : View(delivery);
    }
}