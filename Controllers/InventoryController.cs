using CompanyERP.Entities.Inventory;
using CompanyERP.Interfaces.Services;
using CompanyERP.Services.Company;
using CompanyERP.Services.CompanyBranch;
using CompanyERP.Services.Inventory;
using CompanyERP.ViewModels.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class InventoryController : Controller
{
    private readonly IInventoryService _inventoryService;
    private readonly IProductService _productService;
    private readonly IWarehouseService _warehouseService;
    private readonly ICompanyProfileService _companyService;
    private readonly IBranchService _branchService;

    public InventoryController(
        IInventoryService inventoryService,
        IProductService productService,
        IWarehouseService warehouseService,
        ICompanyProfileService companyService,
        IBranchService branchService)
    {
        _inventoryService = inventoryService;
        _productService = productService;
        _warehouseService = warehouseService;
        _companyService = companyService;
        _branchService = branchService;
    }

    [HttpGet]
    public async Task<IActionResult> Balance(int? productId, int? warehouseId, int? branchId)
    {
        var balances = await _inventoryService.GetBalancesAsync(productId: productId, warehouseId: warehouseId, branchId: branchId);
        await PopulateFiltersAsync(productId, warehouseId, branchId);
        return View(balances);
    }

    [HttpGet]
    public async Task<IActionResult> Transactions(int? productId, int? warehouseId, int? branchId)
    {
        var transactions = await _inventoryService.GetTransactionsAsync(productId: productId, warehouseId: warehouseId, branchId: branchId);
        await PopulateFiltersAsync(productId, warehouseId, branchId);
        return View(transactions);
    }

    [HttpGet]
    public async Task<IActionResult> Transfers()
    {
        var transfers = await _inventoryService.GetTransfersAsync();
        return View(transfers);
    }

    [HttpGet]
    public async Task<IActionResult> StockIn()
    {
        if (!await HasDataAsync())
        {
            TempData["Error"] = "Create product and warehouse before stock-in.";
            return RedirectToAction(nameof(Balance));
        }

        await PopulateTransactionOptionsAsync();
        ViewBag.Types = new SelectList(
            new[]
            {
                StockTransactionType.Opening,
                StockTransactionType.PurchaseReceiving,
                StockTransactionType.SalesReturn,
                StockTransactionType.Adjustment
            }, StockTransactionType.Opening);
        return View(new StockInViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StockIn(StockInViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Types = new SelectList(
                new[]
                {
                    StockTransactionType.Opening,
                    StockTransactionType.PurchaseReceiving,
                    StockTransactionType.SalesReturn,
                    StockTransactionType.Adjustment
                }, model.Type);
            await PopulateTransactionOptionsAsync(model.ProductId, model.WarehouseId);
            return View(model);
        }

        var result = await _inventoryService.StockInAsync(
            GetCompanyId(model.ProductId),
            model.ProductId,
            model.WarehouseId,
            model.Type,
            model.Quantity,
            model.UnitCost,
            model.ReferenceNo,
            model.TransactionDate,
            model.Note);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            ViewBag.Types = new SelectList(
                new[]
                {
                    StockTransactionType.Opening,
                    StockTransactionType.PurchaseReceiving,
                    StockTransactionType.SalesReturn,
                    StockTransactionType.Adjustment
                }, model.Type);
            await PopulateTransactionOptionsAsync(model.ProductId, model.WarehouseId);
            return View(model);
        }

        TempData["Success"] = "Stock added successfully.";
        return RedirectToAction(nameof(Balance));
    }

    [HttpGet]
    public async Task<IActionResult> StockOut()
    {
        if (!await HasDataAsync())
        {
            TempData["Error"] = "Create product and warehouse before stock-out.";
            return RedirectToAction(nameof(Balance));
        }

        await PopulateTransactionOptionsAsync();
        ViewBag.Types = new SelectList(
            new[]
            {
                StockTransactionType.SalesStockOut,
                StockTransactionType.PurchaseReturn,
                StockTransactionType.Adjustment
            }, StockTransactionType.SalesStockOut);
        return View(new StockOutViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StockOut(StockOutViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Types = new SelectList(
                new[]
                {
                    StockTransactionType.SalesStockOut,
                    StockTransactionType.PurchaseReturn,
                    StockTransactionType.Adjustment
                }, model.Type);
            await PopulateTransactionOptionsAsync(model.ProductId, model.WarehouseId);
            return View(model);
        }

        var result = await _inventoryService.StockOutAsync(
            GetCompanyId(model.ProductId),
            model.ProductId,
            model.WarehouseId,
            model.Type,
            model.Quantity,
            model.UnitCost,
            model.ReferenceNo,
            model.TransactionDate,
            model.Note);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            ViewBag.Types = new SelectList(
                new[]
                {
                    StockTransactionType.SalesStockOut,
                    StockTransactionType.PurchaseReturn,
                    StockTransactionType.Adjustment
                }, model.Type);
            await PopulateTransactionOptionsAsync(model.ProductId, model.WarehouseId);
            return View(model);
        }

        TempData["Success"] = "Stock deducted successfully.";
        return RedirectToAction(nameof(Balance));
    }

    [HttpGet]
    public async Task<IActionResult> Transfer()
    {
        if (!await HasDataAsync())
        {
            TempData["Error"] = "Create product and warehouse before transferring stock.";
            return RedirectToAction(nameof(Balance));
        }

        await PopulateTransactionOptionsAsync();
        return View(new StockTransferViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Transfer(StockTransferViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateTransactionOptionsAsync(model.ProductId, model.FromWarehouseId, model.ToWarehouseId);
            return View(model);
        }

        var result = await _inventoryService.TransferAsync(
            GetCompanyId(model.ProductId),
            model.ProductId,
            model.FromWarehouseId,
            model.ToWarehouseId,
            model.Quantity,
            model.TransferDate,
            model.Note);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateTransactionOptionsAsync(model.ProductId, model.FromWarehouseId, model.ToWarehouseId);
            return View(model);
        }

        TempData["Success"] = "Stock transferred successfully.";
        return RedirectToAction(nameof(Transfers));
    }

    [HttpGet]
    public async Task<IActionResult> Adjust()
    {
        if (!await HasDataAsync())
        {
            TempData["Error"] = "Create product and warehouse before adjustment.";
            return RedirectToAction(nameof(Balance));
        }

        await PopulateTransactionOptionsAsync();
        return View(new StockAdjustmentViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Adjust(StockAdjustmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateTransactionOptionsAsync(model.ProductId, model.WarehouseId);
            return View(model);
        }

        var result = await _inventoryService.AdjustAsync(
            GetCompanyId(model.ProductId),
            model.ProductId,
            model.WarehouseId,
            model.Quantity,
            model.ReferenceNo,
            model.TransactionDate,
            model.Note);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateTransactionOptionsAsync(model.ProductId, model.WarehouseId);
            return View(model);
        }

        TempData["Success"] = "Stock adjusted successfully.";
        return RedirectToAction(nameof(Balance));
    }

    private int GetCompanyId(int productId)
    {
        return _productService.GetByIdAsync(productId).GetAwaiter().GetResult()?.CompanyId ?? 0;
    }

    private async Task<bool> HasDataAsync()
    {
        var products = await _productService.GetAllAsync();
        var warehouses = await _warehouseService.GetAllAsync();
        return products.Count > 0 && warehouses.Count > 0;
    }

    private async Task PopulateTransactionOptionsAsync(int? productId = null, int? fromWarehouseId = null, int? toWarehouseId = null)
    {
        var warehouses = await _warehouseService.GetAllAsync();
        ViewBag.Warehouses = new SelectList(warehouses, "Id", "Name", fromWarehouseId);

        var products = await _productService.GetAllAsync();
        ViewBag.Products = new SelectList(products, "Id", "Name", productId);

        if (toWarehouseId.HasValue)
        {
            ViewBag.ToWarehouses = new SelectList(warehouses, "Id", "Name", toWarehouseId.Value);
        }
        else
        {
            ViewBag.ToWarehouses = new SelectList(warehouses, "Id", "Name");
        }

        var companies = await _companyService.GetAllAsync();
        ViewBag.Companies = new SelectList(companies, "Id", "Name");
    }

private async Task PopulateFiltersAsync(int? productId = null, int? warehouseId = null, int? branchId = null)
    {
        var products = await _productService.GetAllAsync();
        ViewBag.Products = new SelectList(products, "Id", "Name", productId);

        var warehouses = await _warehouseService.GetAllAsync();
        ViewBag.Warehouses = new SelectList(warehouses, "Id", "Name", warehouseId);

        var branches = await _branchService.GetAllAsync();
        ViewBag.Branches = new SelectList(branches, "Id", "Name", branchId);
    }
}