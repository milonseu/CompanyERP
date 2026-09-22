using CompanyERP.Entities.Sales;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyERP.Controllers;

public class ServiceController : Controller
{
    private readonly IServiceService _serviceService;
    private readonly ICompanyProfileService _companyService;

    public ServiceController(IServiceService serviceService, ICompanyProfileService companyService)
    {
        _serviceService = serviceService;
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            return View(new List<Service>());
        }
        return View((await _serviceService.GetAllAsync()).Where(s => s.CompanyId == companies.First().Id).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var companies = await _companyService.GetAllAsync();
        if (companies.Count == 0)
        {
            TempData["Error"] = "Create a company before adding a service.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Companies = new SelectList(companies, "Id", "Name", companies.First().Id);
        return View(new Service { CompanyId = companies.First().Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Service model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Companies = new SelectList(await _companyService.GetAllAsync(), "Id", "Name", model.CompanyId);
            return View(model);
        }

        var result = await _serviceService.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            ViewBag.Companies = new SelectList(await _companyService.GetAllAsync(), "Id", "Name", model.CompanyId);
            return View(model);
        }

        TempData["Success"] = "Service created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var model = await _serviceService.GetByIdAsync(id.Value);
        if (model is null)
        {
            return NotFound();
        }

        ViewBag.Companies = new SelectList(await _companyService.GetAllAsync(), "Id", "Name", model.CompanyId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Service model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = new SelectList(await _companyService.GetAllAsync(), "Id", "Name", model.CompanyId);
            return View(model);
        }

        var result = await _serviceService.UpdateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            ViewBag.Companies = new SelectList(await _companyService.GetAllAsync(), "Id", "Name", model.CompanyId);
            return View(model);
        }

        TempData["Success"] = "Service updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var model = await _serviceService.GetByIdAsync(id.Value);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _serviceService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Service deleted.";
        }

        return RedirectToAction(nameof(Index));
    }
}