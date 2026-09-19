using CompanyERP.Entities.CompanyBranch;
using CompanyERP.Entities.Inventory;
using CompanyERP.Entities.Purchase;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class PurchaseOrderController : Controller
{
    private readonly IPurchaseOrderService _orderService;
    private readonly IPurchaseQuotationService _quotationService;
    private readonly ICompanyProfileService _companyService;
    private readonly ISupplierService _supplierService;
    private readonly IBranchService _branchService;
    private readonly IWarehouseService _warehouseService;
    private readonly IProductService _productService;

    public PurchaseOrderController(
        IPurchaseOrderService orderService,
        IPurchaseQuotationService quotationService,
        ICompanyProfileService companyService,
        ISupplierService supplierService,
        IBranchService branchService,
        IWarehouseService warehouseService,
        IProductService productService)
    {
        _orderService = orderService;
        _quotationService = quotationService;
        _companyService = companyService;
        _supplierService = supplierService;
        _branchService = branchService;
        _warehouseService = warehouseService;
        _productService = productService;
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
            TempData["Error"] = "Create a company before adding a purchase order.";
            return RedirectToAction(nameof(Index));
        }

        var model = new PurchaseOrder
        {
            CompanyId = companies.First().Id,
            OrderDate = DateTime.Today,
            Status = PurchaseOrderStatus.Draft
        };
        model.OrderNo = await _orderService.GenerateNumberAsync(model.CompanyId, model.OrderDate);
        await PopulateOptionsAsync(model.CompanyId, null);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseOrder model, List<PurchaseOrderLine> lines)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId, model.SupplierId);
            return View(model);
        }

        var result = await _orderService.CreateAsync(model, lines ?? new List<PurchaseOrderLine>());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId, model.SupplierId);
            return View(model);
        }

        TempData["Success"] = "Purchase order created successfully.";
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

        if (order.Status != PurchaseOrderStatus.Draft)
        {
            TempData["Error"] = "Only draft orders can be edited.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateOptionsAsync(order.CompanyId, order.SupplierId);
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PurchaseOrder model, List<PurchaseOrderLine> lines)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId, model.SupplierId);
            return View(model);
        }

        var result = await _orderService.UpdateAsync(model, lines ?? new List<PurchaseOrderLine>());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId, model.SupplierId);
            return View(model);
        }

        TempData["Success"] = "Purchase order updated successfully.";
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
        if (order is null)
        {
            return NotFound();
        }

        return View(order);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
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

        return View(order);
    }

    [HttpPost, ActionName("Delete")]
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
            TempData["Success"] = "Purchase order deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var result = await _orderService.UpdateStatusAsync(id, PurchaseOrderStatus.Approved);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Purchase order approved.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _orderService.UpdateStatusAsync(id, PurchaseOrderStatus.Cancelled);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Purchase order cancelled.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsAsync(int companyId, int? supplierId)
    {
        var companies = await _companyService.GetAllAsync();
        ViewBag.Companies = new SelectList(companies, "Id", "Name", companyId);

        var suppliers = (await _supplierService.GetAllAsync())
            .Where(s => s.CompanyId == companyId);
        ViewBag.Suppliers = new SelectList(suppliers, "Id", "Name", supplierId);

        var branches = (await _branchService.GetAllAsync())
            .Where(b => b.CompanyId == companyId);
        ViewBag.Branches = new SelectList(branches, "Id", "Name");

        var warehouses = (await _warehouseService.GetByCompanyIdAsync(companyId)).ToList();
        ViewBag.Warehouses = new SelectList(warehouses, "Id", "Name");

        var quotations = (await _quotationService.GetAllAsync())
            .Where(q => q.CompanyId == companyId
                && q.Status is PurchaseQuotationStatus.Draft or PurchaseQuotationStatus.Sent);
        ViewBag.Quotations = new SelectList(quotations, "Id", "QuotationNo");

        var products = (await _productService.GetAllAsync())
            .Where(p => p.CompanyId == companyId);
        ViewBag.Products = new SelectList(products, "Id", "Name");
        ViewBag.ProductsJson = System.Text.Json.JsonSerializer.Serialize(
            products.Select(p => new { p.Id, p.Name }).ToList());
    }
}