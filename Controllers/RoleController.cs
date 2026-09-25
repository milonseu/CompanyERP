using CompanyERP.Entities.Security;
using CompanyERP.Interfaces.Services;
using CompanyERP.ViewModels.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompanyERP.Controllers;

[Authorize]
[HasPermission("Security.View")]
public class RoleController : Controller
{
    private readonly IRoleService _roleService;
    private readonly IPermissionService _permissionService;
    private readonly IActivityLogService _activityLogService;

    public RoleController(
        IRoleService roleService,
        IPermissionService permissionService,
        IActivityLogService activityLogService)
    {
        _roleService = roleService;
        _permissionService = permissionService;
        _activityLogService = activityLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var roles = await _roleService.GetAllAsync();
        return View(roles);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new RoleFormViewModel
        {
            AllPermissions = await _permissionService.GetAllAsync(),
            Modules = await _permissionService.GetModulesAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Security.Create")]
    public async Task<IActionResult> Create(RoleFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AllPermissions = await _permissionService.GetAllAsync();
            model.Modules = await _permissionService.GetModulesAsync();
            return View(model);
        }

        var result = await _roleService.CreateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            model.AllPermissions = await _permissionService.GetAllAsync();
            model.Modules = await _permissionService.GetModulesAsync();
            return View(model);
        }

        var role = await _roleService.GetByCodeAsync(model.Code);
        if (role is not null)
        {
            await _roleService.SetRolePermissionsAsync(role.Id, model.SelectedPermissionIds);
        }

        await _activityLogService.LogAsync(User.Identity?.Name ?? "System", "Create", "Security", nameof(Role), role?.Id, $"Role '{model.Code}' created.", HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData["Success"] = "Role created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var role = await _roleService.GetByIdAsync(id.Value);
        if (role is null)
        {
            return NotFound();
        }

        var model = RoleFormViewModel.FromEntity(role);
        model.AllPermissions = await _permissionService.GetAllAsync();
        model.Modules = await _permissionService.GetModulesAsync();
        model.SelectedPermissionIds = await _roleService.GetAssignedPermissionIdsAsync(role.Id);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Security.Edit")]
    public async Task<IActionResult> Edit(int id, RoleFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.AllPermissions = await _permissionService.GetAllAsync();
            model.Modules = await _permissionService.GetModulesAsync();
            return View(model);
        }

        var result = await _roleService.UpdateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            model.AllPermissions = await _permissionService.GetAllAsync();
            model.Modules = await _permissionService.GetModulesAsync();
            return View(model);
        }

        await _roleService.SetRolePermissionsAsync(id, model.SelectedPermissionIds);

        await _activityLogService.LogAsync(User.Identity?.Name ?? "System", "Update", "Security", nameof(Role), id, $"Role '{model.Code}' updated.", HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData["Success"] = "Role updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var role = await _roleService.GetByIdAsync(id.Value);
        if (role is null)
        {
            return NotFound();
        }

        ViewBag.Permissions = await _roleService.GetRolePermissionsAsync(role.Id);
        return View(role);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var role = await _roleService.GetByIdAsync(id.Value);
        if (role is null)
        {
            return NotFound();
        }

        ViewBag.HasUsers = await _roleService.HasUsersAsync(id.Value);
        return View(role);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [HasPermission("Security.Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var code = (await _roleService.GetByIdAsync(id))?.Code ?? string.Empty;
        var result = await _roleService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            await _activityLogService.LogAsync(User.Identity?.Name ?? "System", "Delete", "Security", nameof(Role), id, $"Role '{code}' deleted.", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = "Role deleted.";
        }

        return RedirectToAction(nameof(Index));
    }
}