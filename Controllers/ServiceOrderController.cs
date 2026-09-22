using CompanyERP.Entities.Sales;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class ServiceOrderController : Controller
{
    private readonly IServiceOrderService _orderService;
    private readonly ICompanyProfileService _companyService;
    private readonly ICustomerService _customerService;
    private readonly IBranchService _branchService;
    private readonly IServiceService _serviceService;

    public ServiceOrderController(
        IServiceOrderService orderService,
        ICompanyProfileService companyService,
        ICustomerService customerService,
        IBranchService branchService,
        IServiceService serviceService)
    {
        _orderService = orderService;
        _companyService = companyService;
        _customerService = customerService;
        _branchService = branchService;
        _serviceService = serviceService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await _orderService.GetAllAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            TempData["Error"] = "Create a company before adding a service order.";
            return RedirectToAction(nameof(Index));
        }

        var model = new ServiceOrder
        {
            CompanyId = companies.First().Id,
            OrderDate = DateTime.Today,
            Status = ServiceOrderStatus.Pending,
            Quantity = 1
        };
        model.OrderNo = await _orderService.GenerateNumberAsync(model.CompanyId, model.OrderDate);
        await PopulateOptionsAsync(model.CompanyId, null);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceOrder model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId, model.CustomerId);
            return View(model);
        }

        var result = await _orderService.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId, model.CustomerId);
            return View(model);
        }

        TempData["Success"] = "Service order created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var order = await _orderService.GetByIdAsync(id.Value);
        return order is null ? NotFound() : View(order);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var order = await _orderService.GetByIdAsync(id.Value);
        return order is null ? NotFound() : View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _orderService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Service order deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id)
    {
        var result = await _orderService.UpdateStatusAsync(id, ServiceOrderStatus.InProgress);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Service order started.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _orderService.UpdateStatusAsync(id, ServiceOrderStatus.Cancelled);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Service order cancelled.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsAsync(int companyId, int? customerId)
    {
        var companies = await _companyService.GetAllAsync();
        ViewBag.Companies = new SelectList(companies, "Id", "Name", companyId);

        var customers = (await _customerService.GetAllAsync()).Where(c => c.CompanyId == companyId);
        ViewBag.Customers = new SelectList(customers, "Id", "Name", customerId);

        var branches = (await _branchService.GetAllAsync()).Where(b => b.CompanyId == companyId);
        ViewBag.Branches = new SelectList(branches, "Id", "Name");

var services = (await _serviceService.GetByCompanyIdAsync(companyId)).ToList();
        ViewBag.Services = new SelectList(services, "Id", "Name");
        ViewBag.ServicesJson = System.Text.Json.JsonSerializer.Serialize(
            services.Select(s => new { s.Id, s.UnitPrice }));
    }
}