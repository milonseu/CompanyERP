using CompanyERP.Interfaces.Services;
using CompanyERP.Services.Company;
using CompanyERP.Services.Customer;
using CompanyERP.ViewModels.Customer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class CustomerController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly ICompanyProfileService _companyService;

    public CustomerController(ICustomerService customerService, ICompanyProfileService companyService)
    {
        _customerService = customerService;
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var customers = await _customerService.GetAllAsync();
        return View(customers);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!await HasCompaniesAsync())
        {
            TempData["Error"] = "Create a company before adding a customer.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateOptionsAsync();
        return View(new CustomerFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model.CompanyId);
            return View(model);
        }

        var result = await _customerService.CreateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId);
            return View(model);
        }

        TempData["Success"] = "Customer created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var customer = await _customerService.GetByIdAsync(id.Value);
        if (customer is null)
        {
            return NotFound();
        }

        if (!await HasCompaniesAsync())
        {
            TempData["Error"] = "Create a company before editing a customer.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateOptionsAsync(customer.CompanyId);
        return View(CustomerFormViewModel.FromEntity(customer));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerFormViewModel model)
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

        var result = await _customerService.UpdateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(model.CompanyId);
            return View(model);
        }

        TempData["Success"] = "Customer updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var customer = await _customerService.GetByIdAsync(id.Value);
        if (customer is null)
        {
            return NotFound();
        }

        return View(customer);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var customer = await _customerService.GetByIdAsync(id.Value);
        if (customer is null)
        {
            return NotFound();
        }

        return View(customer);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _customerService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Customer deleted.";
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

        var customer = await _customerService.GetByIdAsync(id.Value);
        if (customer is null)
        {
            return NotFound();
        }

        ViewBag.CustomerName = customer.Name;
        ViewBag.Contacts = await _customerService.GetContactsAsync(id.Value);
        return View(new CustomerContactFormViewModel { CustomerId = id.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateContact(CustomerContactFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var customer = await _customerService.GetByIdAsync(model.CustomerId);
            ViewBag.CustomerName = customer?.Name ?? string.Empty;
            ViewBag.Contacts = await _customerService.GetContactsAsync(model.CustomerId);
            return View(nameof(Contacts), model);
        }

        var result = await _customerService.AddContactAsync(model.ToEntity());
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Contact added successfully.";
        }

        return RedirectToAction(nameof(Contacts), new { id = model.CustomerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteContact(int id, int customerId)
    {
        var result = await _customerService.DeleteContactAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Contact removed.";
        }

        return RedirectToAction(nameof(Contacts), new { id = customerId });
    }

    // ---- Addresses ----

    [HttpGet]
    public async Task<IActionResult> Addresses(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var customer = await _customerService.GetByIdAsync(id.Value);
        if (customer is null)
        {
            return NotFound();
        }

        ViewBag.CustomerName = customer.Name;
        ViewBag.Addresses = await _customerService.GetAddressesAsync(id.Value);
        return View(new CustomerAddressFormViewModel { CustomerId = id.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAddress(CustomerAddressFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var customer = await _customerService.GetByIdAsync(model.CustomerId);
            ViewBag.CustomerName = customer?.Name ?? string.Empty;
            ViewBag.Addresses = await _customerService.GetAddressesAsync(model.CustomerId);
            return View(nameof(Addresses), model);
        }

        var result = await _customerService.AddAddressAsync(model.ToEntity());
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Address added successfully.";
        }

        return RedirectToAction(nameof(Addresses), new { id = model.CustomerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(int id, int customerId)
    {
        var result = await _customerService.DeleteAddressAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Address removed.";
        }

        return RedirectToAction(nameof(Addresses), new { id = customerId });
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