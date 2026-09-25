using CompanyERP.Entities.Security;
using CompanyERP.Interfaces.Services;
using CompanyERP.ViewModels.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompanyERP.Controllers;

[Authorize]
[HasPermission("Security.View")]
public class PermissionController : Controller
{
    private readonly IPermissionService _permissionService;
    private readonly IActivityLogService _activityLogService;

    public PermissionController(IPermissionService permissionService, IActivityLogService activityLogService)
    {
        _permissionService = permissionService;
        _activityLogService = activityLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var permissions = await _permissionService.GetAllAsync();
        return View(permissions);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new PermissionFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Security.Create")]
    public async Task<IActionResult> Create(PermissionFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _permissionService.CreateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        await _activityLogService.LogAsync(User.Identity?.Name ?? "System", "Create", "Security", nameof(Permission), null, $"Permission '{model.Code}' created.", HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData["Success"] = "Permission created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var permission = await _permissionService.GetByIdAsync(id.Value);
        if (permission is null)
        {
            return NotFound();
        }

        return View(PermissionFormViewModel.FromEntity(permission));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Security.Edit")]
    public async Task<IActionResult> Edit(int id, PermissionFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _permissionService.UpdateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        await _activityLogService.LogAsync(User.Identity?.Name ?? "System", "Update", "Security", nameof(Permission), id, $"Permission '{model.Code}' updated.", HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData["Success"] = "Permission updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var permission = await _permissionService.GetByIdAsync(id.Value);
        if (permission is null)
        {
            return NotFound();
        }

        return View(permission);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [HasPermission("Security.Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var code = (await _permissionService.GetByIdAsync(id))?.Code ?? string.Empty;
        var result = await _permissionService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            await _activityLogService.LogAsync(User.Identity?.Name ?? "System", "Delete", "Security", nameof(Permission), id, $"Permission '{code}' deleted.", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = "Permission deleted.";
        }

        return RedirectToAction(nameof(Index));
    }
}