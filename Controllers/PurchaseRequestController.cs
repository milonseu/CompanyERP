using CompanyERP.Entities.CompanyBranch;
using CompanyERP.Entities.Inventory;
using CompanyERP.Entities.Purchase;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class PurchaseRequestController : Controller
{
    private readonly IPurchaseRequestService _requestService;
    private readonly ICompanyProfileService _companyService;
    private readonly IBranchService _branchService;
    private readonly IProductService _productService;

    public PurchaseRequestController(
        IPurchaseRequestService requestService,
        ICompanyProfileService companyService,
        IBranchService branchService,
        IProductService productService)
    {
        _requestService = requestService;
        _companyService = companyService;
        _branchService = branchService;
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await _requestService.GetAllAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!await HasCompaniesAsync())
        {
            TempData["Error"] = "Create a company before adding a purchase request.";
            return RedirectToAction(nameof(Index));
        }

        var model = new PurchaseRequest();
        var companies = await _companyService.GetAllAsync();
        model.CompanyId = companies.First().Id;
        model.RequestNo = await _requestService.GenerateNumberAsync(model.CompanyId, model.RequestDate);
        await PopulateOptionsAsync(model.CompanyId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseRequest model, List<PurchaseRequestLine> lines)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId);
            return View(model);
        }

        var result = await _requestService.CreateAsync(model, lines ?? new List<PurchaseRequestLine>());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId);
            return View(model);
        }

        TempData["Success"] = "Purchase request created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var request = await _requestService.GetByIdAsync(id.Value);
        if (request is null)
        {
            return NotFound();
        }

        if (request.Status != PurchaseRequestStatus.Draft)
        {
            TempData["Error"] = "Only draft requests can be edited.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateOptionsAsync(request.CompanyId);
        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PurchaseRequest model, List<PurchaseRequestLine> lines)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId);
            return View(model);
        }

        var result = await _requestService.UpdateAsync(model, lines ?? new List<PurchaseRequestLine>());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId);
            return View(model);
        }

        TempData["Success"] = "Purchase request updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var request = await _requestService.GetByIdAsync(id.Value);
        if (request is null)
        {
            return NotFound();
        }

        return View(request);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var request = await _requestService.GetByIdAsync(id.Value);
        if (request is null)
        {
            return NotFound();
        }

        return View(request);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _requestService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Purchase request deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var result = await _requestService.UpdateStatusAsync(id, PurchaseRequestStatus.Approved);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Purchase request approved.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _requestService.UpdateStatusAsync(id, PurchaseRequestStatus.Cancelled);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Purchase request cancelled.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> HasCompaniesAsync()
    {
        var companies = await _companyService.GetAllAsync();
        return companies.Count > 0;
    }

    private async Task PopulateOptionsAsync(int companyId)
    {
        var companies = await _companyService.GetAllAsync();
        ViewBag.Companies = new SelectList(companies, "Id", "Name", companyId);

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