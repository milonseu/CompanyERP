using CompanyERP.Entities.Payment;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class PaymentMethodController : Controller
{
    private readonly IPaymentMethodService _methodService;
    private readonly ICompanyProfileService _companyService;

    public PaymentMethodController(IPaymentMethodService methodService, ICompanyProfileService companyService)
    {
        _methodService = methodService;
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            return View(new List<PaymentMethod>());
        }

        var companyId = companies.First().Id;
        return View(await _methodService.GetAllAsync(companyId));
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            TempData["Error"] = "Create a company before adding a payment method.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateOptionsAsync(companies);
        return View(new PaymentMethod { CompanyId = companies.First().Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentMethod model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(await _companyService.GetAllAsync());
            return View(model);
        }

        var result = await _methodService.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(await _companyService.GetAllAsync());
            return View(model);
        }

        TempData["Success"] = "Payment method created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var model = await _methodService.GetByIdAsync(id.Value);
        if (model is null)
        {
            return NotFound();
        }

        await PopulateOptionsAsync(await _companyService.GetAllAsync());
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PaymentMethod model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(await _companyService.GetAllAsync());
            return View(model);
        }

        var result = await _methodService.UpdateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateOptionsAsync(await _companyService.GetAllAsync());
            return View(model);
        }

        TempData["Success"] = "Payment method updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var model = await _methodService.GetByIdAsync(id.Value);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _methodService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Payment method deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    private Task PopulateOptionsAsync(List<Entities.Company.CompanyProfile> companies)
    {
        ViewBag.Companies = new SelectList(companies, "Id", "Name");
        return Task.CompletedTask;
    }
}