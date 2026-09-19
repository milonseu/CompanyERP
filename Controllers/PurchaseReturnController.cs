using CompanyERP.Entities.CompanyBranch;
using CompanyERP.Entities.Inventory;
using CompanyERP.Entities.Purchase;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class PurchaseReturnController : Controller
{
    private readonly IPurchaseReturnService _returnService;
    private readonly IPurchaseInvoiceService _invoiceService;
    private readonly ICompanyProfileService _companyService;
    private readonly ISupplierService _supplierService;
    private readonly IBranchService _branchService;
    private readonly IWarehouseService _warehouseService;
    private readonly IProductService _productService;

    public PurchaseReturnController(
        IPurchaseReturnService returnService,
        IPurchaseInvoiceService invoiceService,
        ICompanyProfileService companyService,
        ISupplierService supplierService,
        IBranchService branchService,
        IWarehouseService warehouseService,
        IProductService productService)
    {
        _returnService = returnService;
        _invoiceService = invoiceService;
        _companyService = companyService;
        _supplierService = supplierService;
        _branchService = branchService;
        _warehouseService = warehouseService;
        _productService = productService;
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
            TempData["Error"] = "Create a company before adding a purchase return.";
            return RedirectToAction(nameof(Index));
        }

        var model = new PurchaseReturn
        {
            CompanyId = companies.First().Id,
            ReturnDate = DateTime.Today
        };
        await PopulateOptionsAsync(model.CompanyId, null);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseReturn model, List<PurchaseReturnLine> lines)
    {
        ModelState.Remove(nameof(PurchaseReturn.ReturnNo));

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId, model.PurchaseInvoiceId);
            return View(model);
        }

        var result = await _returnService.CreateAsync(model, lines ?? new List<PurchaseReturnLine>());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId, model.PurchaseInvoiceId);
            return View(model);
        }

        TempData["Success"] = "Purchase return created and stock updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var purchaseReturn = await _returnService.GetByIdAsync(id.Value);
        if (purchaseReturn is null)
        {
            return NotFound();
        }

        return View(purchaseReturn);
    }

    [HttpGet]
    public async Task<IActionResult> GetInvoiceLines(int invoiceId)
    {
        var invoice = await _invoiceService.GetByIdAsync(invoiceId);
        if (invoice is null)
        {
            return Json(new { Success = false });
        }

        var lines = invoice.Lines.Select(l => new
        {
            l.ProductId,
            ProductName = l.Product != null ? l.Product.Name : "N/A",
            l.Quantity,
            l.UnitPrice
        }).ToList();

        return Json(new
        {
            Success = true,
            CompanyId = invoice.CompanyId,
            SupplierId = invoice.SupplierId,
            SupplierName = invoice.Supplier != null ? invoice.Supplier.Name : "N/A",
            InvoiceNo = invoice.InvoiceNo,
            Lines = lines
        });
    }

    private async Task PopulateOptionsAsync(int companyId, int? invoiceId)
    {
        var companies = await _companyService.GetAllAsync();
        ViewBag.Companies = new SelectList(companies, "Id", "Name", companyId);

        var suppliers = (await _supplierService.GetAllAsync())
            .Where(s => s.CompanyId == companyId);
        ViewBag.Suppliers = new SelectList(suppliers, "Id", "Name");

        var invoices = (await _invoiceService.GetAllAsync())
            .Where(i => i.CompanyId == companyId && i.Lines.Count > 0);
        ViewBag.Invoices = new SelectList(invoices, "Id", "InvoiceNo", invoiceId);

        var warehouses = (await _warehouseService.GetByCompanyIdAsync(companyId)).ToList();
        ViewBag.Warehouses = new SelectList(warehouses, "Id", "Name");

        var branches = (await _branchService.GetAllAsync())
            .Where(b => b.CompanyId == companyId);
        ViewBag.Branches = new SelectList(branches, "Id", "Name");

        var products = (await _productService.GetAllAsync())
            .Where(p => p.CompanyId == companyId);
        ViewBag.Products = new SelectList(products, "Id", "Name");
        ViewBag.ProductsJson = System.Text.Json.JsonSerializer.Serialize(
            products.Select(p => new { p.Id, p.Name }).ToList());
    }
}