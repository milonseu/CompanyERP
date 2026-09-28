using CompanyERP.Entities.Inventory;
using CompanyERP.Interfaces.Services;
using CompanyERP.Services.Company;
using CompanyERP.Services.CompanyBranch;
using CompanyERP.Services.Inventory;
using CompanyERP.ViewModels.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

[HasPermission("Inventory.View")]
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

    /// <summary>
    /// A manual stock-in is either opening stock or a surplus adjustment, and both post their own
    /// journal. Purchase receipts and sales returns are entered by their modules, which post the
    /// matching payable or receivable entry, so they are not offered here.
    /// </summary>
    private static readonly StockTransactionType[] ManualStockInTypes =
    {
        StockTransactionType.Opening,
        StockTransactionType.Adjustment
    };

    [HttpGet]
    [HasPermission("Inventory.StockIn")]
    public async Task<IActionResult> StockIn()
    {
        if (!await HasDataAsync())
        {
            TempData["Error"] = "Create product and warehouse before stock-in.";
            return RedirectToAction(nameof(Balance));
        }

        await PopulateTransactionOptionsAsync();
        ViewBag.Types = new SelectList(ManualStockInTypes, StockTransactionType.Opening);
        return View(new StockInViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Inventory.StockIn")]
    public async Task<IActionResult> StockIn(StockInViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Types = new SelectList(ManualStockInTypes, model.Type);
            await PopulateTransactionOptionsAsync(model.ProductId, model.WarehouseId);
            return View(model);
        }

        if (!ManualStockInTypes.Contains(model.Type))
        {
            ModelState.AddModelError(string.Empty,
                "This type is created by its own module. Receive the goods through purchase, or return them through sales, so the payable or receivable is posted with the stock.");
            ViewBag.Types = new SelectList(ManualStockInTypes, model.Type);
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
            ViewBag.Types = new SelectList(ManualStockInTypes, model.Type);
            await PopulateTransactionOptionsAsync(model.ProductId, model.WarehouseId);
            return View(model);
        }

        TempData["Success"] =
            $"Stock added successfully. New average cost: {result.AverageCost:0.00}. A journal entry was posted for this {model.Type} movement.";
        return RedirectToAction(nameof(Balance));
    }

    /// <summary>
    /// A manual stock-out can only be a write-off. Sales issues and purchase returns belong to the
    /// sales and purchase modules because they need the customer or supplier document to post a
    /// matching receivable or payable; creating them here would move stock with no journal behind it.
    /// </summary>
    private static readonly StockTransactionType[] ManualStockOutTypes =
    {
        StockTransactionType.Adjustment
    };

    [HttpGet]
    [HasPermission("Inventory.StockOut")]
    public async Task<IActionResult> StockOut()
    {
        if (!await HasDataAsync())
        {
            TempData["Error"] = "Create product and warehouse before stock-out.";
            return RedirectToAction(nameof(Balance));
        }

        await PopulateTransactionOptionsAsync();
        ViewBag.Types = new SelectList(ManualStockOutTypes, StockTransactionType.Adjustment);
        return View(new StockOutViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Inventory.StockOut")]
    public async Task<IActionResult> StockOut(StockOutViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Types = new SelectList(ManualStockOutTypes, model.Type);
            await PopulateTransactionOptionsAsync(model.ProductId, model.WarehouseId);
            return View(model);
        }

        if (!ManualStockOutTypes.Contains(model.Type))
        {
            ModelState.AddModelError(string.Empty,
                "This type is created by its own module. Record a stock adjustment here, or use the sales or purchase document it belongs to.");
            ViewBag.Types = new SelectList(ManualStockOutTypes, model.Type);
            await PopulateTransactionOptionsAsync(model.ProductId, model.WarehouseId);
            return View(model);
        }

        var result = await _inventoryService.StockOutAsync(
            GetCompanyId(model.ProductId),
            model.ProductId,
            model.WarehouseId,
            model.Type,
            model.Quantity,
            model.ReferenceNo,
            model.TransactionDate,
            model.Note);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            ViewBag.Types = new SelectList(ManualStockOutTypes, model.Type);
            await PopulateTransactionOptionsAsync(model.ProductId, model.WarehouseId);
            var balance = await _inventoryService.GetBalanceAsync(model.ProductId, model.WarehouseId);
            model.AvailableQty = balance?.Quantity ?? 0m;
            model.AvailableAvgCost = balance?.AverageCost ?? 0m;
            return View(model);
        }

        var relieved = Math.Round(model.Quantity * result.UnitCost, 2, MidpointRounding.AwayFromZero);
        TempData["Success"] = $"Stock deducted successfully. {model.Quantity} at {result.UnitCost:0.00} ({relieved:N2} relieved at average cost).";
        return RedirectToAction(nameof(Balance));
    }

    [HttpGet]
    [HasPermission("Inventory.StockTransfer")]
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
    [HasPermission("Inventory.StockTransfer")]
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
    [HasPermission("Inventory.Adjust")]
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
    [HasPermission("Inventory.Adjust")]
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
            var balance = await _inventoryService.GetBalanceAsync(model.ProductId, model.WarehouseId);
            model.CurrentQty = balance?.Quantity ?? 0m;
            model.CurrentAvgCost = balance?.AverageCost ?? 0m;
            return View(model);
        }

        var direction = result.ValueEffect < 0 ? "shortage written off" : "surplus recognised as gain";
        TempData["Success"] = $"Stock adjusted successfully. {Math.Abs(result.ValueEffect):N2} {direction}.";
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