using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class InventoryReportController : Controller
{
    private readonly IInventoryReportService _reportService;
    private readonly ICompanyProfileService _companyService;
    private readonly IProductCategoryService _categoryService;
    private readonly IWarehouseService _warehouseService;

    public InventoryReportController(
        IInventoryReportService reportService,
        ICompanyProfileService companyService,
        IProductCategoryService categoryService,
        IWarehouseService warehouseService)
    {
        _reportService = reportService;
        _companyService = companyService;
        _categoryService = categoryService;
        _warehouseService = warehouseService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    private async Task<int> GetCompanyIdAsync()
    {
        var companies = await _companyService.GetAllAsync();
        return companies.Count == 0 ? 0 : companies.First().Id;
    }

    private DateTime? ParseDate(string? value)
    {
        return DateTime.TryParse(value, out var parsed) ? parsed : null;
    }

    [HttpGet]
    public async Task<IActionResult> Stock(int? categoryId, int? warehouseId)
    {
        var companyId = await GetCompanyIdAsync();
        var vm = await _reportService.GetStockReportAsync(companyId, categoryId, warehouseId);
        ViewBag.Categories = new SelectList(
            (await _categoryService.GetAllAsync()).Where(c => c.CompanyId == companyId),
            "Id", "Name", categoryId);
        ViewBag.Warehouses = new SelectList(
            (await _warehouseService.GetAllAsync()).Where(w => w.CompanyId == companyId),
            "Id", "Name", warehouseId);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Movement(string? fromDate, string? toDate)
    {
        var vm = await _reportService.GetStockMovementAsync(await GetCompanyIdAsync(), ParseDate(fromDate), ParseDate(toDate));
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> LowStock(int? categoryId)
    {
        var companyId = await GetCompanyIdAsync();
        var vm = await _reportService.GetLowStockAsync(companyId, categoryId);
        ViewBag.Categories = new SelectList(
            (await _categoryService.GetAllAsync()).Where(c => c.CompanyId == companyId),
            "Id", "Name", categoryId);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> WarehouseStock()
    {
        var vm = await _reportService.GetWarehouseStockAsync(await GetCompanyIdAsync());
        return View(vm);
    }
}