using CompanyERP.Entities.Sales;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class SalesOrderController : Controller
{
    private readonly ISalesOrderService _orderService;
    private readonly ICompanyProfileService _companyService;
    private readonly ICustomerService _customerService;
    private readonly IBranchService _branchService;
    private readonly IProductService _productService;
    private readonly IServiceService _serviceService;

    public SalesOrderController(
        ISalesOrderService orderService,
        ICompanyProfileService companyService,
        ICustomerService customerService,
        IBranchService branchService,
        IProductService productService,
        IServiceService serviceService)
    {
        _orderService = orderService;
        _companyService = companyService;
        _customerService = customerService;
        _branchService = branchService;
        _productService = productService;
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
            TempData["Error"] = "Create a company before adding a sales order.";
            return RedirectToAction(nameof(Index));
        }

        var model = new SalesOrder
        {
            CompanyId = companies.First().Id,
            OrderDate = DateTime.Today,
            Status = SalesOrderStatus.Draft
        };
        model.OrderNo = await _orderService.GenerateNumberAsync(model.CompanyId, model.OrderDate);
        await PopulateOptionsAsync(model.CompanyId, null);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SalesOrder model, List<SalesOrderLine> lines)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId, model.CustomerId);
            return View(model);
        }

        var result = await _orderService.CreateAsync(model, lines ?? new List<SalesOrderLine>());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId, model.CustomerId);
            return View(model);
        }

        TempData["Success"] = "Sales order created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var order = await _orderService.GetByIdAsync(id.Value);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != SalesOrderStatus.Draft)
        {
            TempData["Error"] = "Only draft orders can be edited.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateOptionsAsync(order.CompanyId, order.CustomerId);
        ViewBag.ExistingLinesJson = System.Text.Json.JsonSerializer.Serialize(
            order.Lines.Select(l => new
            {
                itemType = (int)l.ItemType,
                productId = l.ProductId,
                serviceId = l.ServiceId,
                quantity = l.Quantity,
                unitPrice = l.UnitPrice
            }));
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SalesOrder model, List<SalesOrderLine> lines)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId, model.CustomerId);
            ViewBag.ExistingLinesJson = "[]";
            return View(model);
        }

        var result = await _orderService.UpdateAsync(model, lines ?? new List<SalesOrderLine>());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId, model.CustomerId);
            ViewBag.ExistingLinesJson = "[]";
            return View(model);
        }

        TempData["Success"] = "Sales order updated successfully.";
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
            TempData["Success"] = "Sales order deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        var result = await _orderService.UpdateStatusAsync(id, SalesOrderStatus.Confirmed);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Sales order confirmed.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _orderService.UpdateStatusAsync(id, SalesOrderStatus.Cancelled);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Sales order cancelled.";
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

        var products = (await _productService.GetByCompanyIdAsync(companyId)).ToList();
        ViewBag.Products = new SelectList(products, "Id", "Name");
        ViewBag.ProductsJson = System.Text.Json.JsonSerializer.Serialize(
            products.Select(p => new { p.Id, p.Name, SalePrice = p.SalePrice }));

        var services = (await _serviceService.GetByCompanyIdAsync(companyId)).ToList();
        ViewBag.Services = new SelectList(services, "Id", "Name");
        ViewBag.ServicesJson = System.Text.Json.JsonSerializer.Serialize(
            services.Select(s => new { s.Id, s.Name, ServiceKind = (int)s.ServiceKind, s.UnitPrice }));
    }
}