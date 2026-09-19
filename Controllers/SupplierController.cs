using CompanyERP.Interfaces.Services;
using CompanyERP.Services.Company;
using CompanyERP.Services.Supplier;
using CompanyERP.ViewModels.Supplier;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class SupplierController : Controller
{
    private readonly ISupplierService _supplierService;
    private readonly ICompanyProfileService _companyService;

    public SupplierController(ISupplierService supplierService, ICompanyProfileService companyService)
    {
        _supplierService = supplierService;
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var suppliers = await _supplierService.GetAllAsync();
        return View(suppliers);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!await HasCompaniesAsync())
        {
            TempData["Error"] = "Create a company before adding a supplier.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateOptionsAsync();
        return View(new SupplierFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId);
            return View(model);
        }

        var result = await _supplierService.CreateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId);
            return View(model);
        }

        TempData["Success"] = "Supplier created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var supplier = await _supplierService.GetByIdAsync(id.Value);
        if (supplier is null)
        {
            return NotFound();
        }

        if (!await HasCompaniesAsync())
        {
            TempData["Error"] = "Create a company before editing a supplier.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateOptionsAsync(supplier.CompanyId);
        return View(SupplierFormViewModel.FromEntity(supplier));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SupplierFormViewModel model)
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

        var result = await _supplierService.UpdateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId);
            return View(model);
        }

        TempData["Success"] = "Supplier updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var supplier = await _supplierService.GetByIdAsync(id.Value);
        if (supplier is null)
        {
            return NotFound();
        }

        return View(supplier);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var supplier = await _supplierService.GetByIdAsync(id.Value);
        if (supplier is null)
        {
            return NotFound();
        }

        return View(supplier);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _supplierService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Supplier deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    // ---- Contacts ----

    [HttpGet]
    public async Task<IActionResult> Contacts(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var supplier = await _supplierService.GetByIdAsync(id.Value);
        if (supplier is null)
        {
            return NotFound();
        }

        ViewBag.SupplierName = supplier.Name;
        ViewBag.Contacts = await _supplierService.GetContactsAsync(id.Value);
        return View(new SupplierContactFormViewModel { SupplierId = id.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateContact(SupplierContactFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var supplier = await _supplierService.GetByIdAsync(model.SupplierId);
            ViewBag.SupplierName = supplier?.Name ?? string.Empty;
            ViewBag.Contacts = await _supplierService.GetContactsAsync(model.SupplierId);
            return View(nameof(Contacts), model);
        }

        var result = await _supplierService.AddContactAsync(model.ToEntity());
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Contact added successfully.";
        }

        return RedirectToAction(nameof(Contacts), new { id = model.SupplierId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteContact(int id, int supplierId)
    {
        var result = await _supplierService.DeleteContactAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Contact removed.";
        }

        return RedirectToAction(nameof(Contacts), new { id = supplierId });
    }

    // ---- Addresses ----

    [HttpGet]
    public async Task<IActionResult> Addresses(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var supplier = await _supplierService.GetByIdAsync(id.Value);
        if (supplier is null)
        {
            return NotFound();
        }

        ViewBag.SupplierName = supplier.Name;
        ViewBag.Addresses = await _supplierService.GetAddressesAsync(id.Value);
        return View(new SupplierAddressFormViewModel { SupplierId = id.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAddress(SupplierAddressFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var supplier = await _supplierService.GetByIdAsync(model.SupplierId);
            ViewBag.SupplierName = supplier?.Name ?? string.Empty;
            ViewBag.Addresses = await _supplierService.GetAddressesAsync(model.SupplierId);
            return View(nameof(Addresses), model);
        }

        var result = await _supplierService.AddAddressAsync(model.ToEntity());
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Address added successfully.";
        }

        return RedirectToAction(nameof(Addresses), new { id = model.SupplierId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(int id, int supplierId)
    {
        var result = await _supplierService.DeleteAddressAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Address removed.";
        }

        return RedirectToAction(nameof(Addresses), new { id = supplierId });
    }

    private async Task<bool> HasCompaniesAsync()
    {
        var companies = await _companyService.GetAllAsync();
        return companies.Count > 0;
    }

    private async Task PopulateOptionsAsync(int? companyId = null)
    {
        var companies = await _companyService.GetAllAsync();
        ViewBag.Companies = new SelectList(companies, "Id", "Name", companyId);
    }
}