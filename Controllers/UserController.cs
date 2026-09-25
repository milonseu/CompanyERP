using System.Security.Claims;
using CompanyERP.Interfaces.Services;
using CompanyERP.ViewModels.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompanyERP.Controllers;

[Authorize]
[HasPermission("Security.View")]
public class UserController : Controller
{
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;
    private readonly IPermissionService _permissionService;
    private readonly IActivityLogService _activityLogService;

    public UserController(
        IUserService userService,
        IRoleService roleService,
        IPermissionService permissionService,
        IActivityLogService activityLogService)
    {
        _userService = userService;
        _roleService = roleService;
        _permissionService = permissionService;
        _activityLogService = activityLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userService.GetAllAsync();
        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new UserFormViewModel
        {
            AllRoles = await _roleService.GetAllAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Security.Create")]
    public async Task<IActionResult> Create(UserFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AllRoles = await _roleService.GetAllAsync();
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Password is required.");
            model.AllRoles = await _roleService.GetAllAsync();
            return View(model);
        }

        var result = await _userService.CreateAsync(model.ToEntity(), model.Password);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            model.AllRoles = await _roleService.GetAllAsync();
            return View(model);
        }

        var user = await _userService.GetByUserNameAsync(model.UserName);
        if (user is not null)
        {
            await _userService.SetUserRolesAsync(user.Id, model.SelectedRoleIds);
            await _userService.SetUserPermissionsAsync(user.Id, model.SelectedPermissionIds);
        }

        await _activityLogService.LogAsync(User.Identity?.Name ?? "System", "Create", "Security", nameof(User), user?.Id, $"User '{model.UserName}' created.", HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData["Success"] = "User created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var user = await _userService.GetByIdAsync(id.Value);
        if (user is null)
        {
            return NotFound();
        }

        var model = UserFormViewModel.FromEntity(user);
        model.AllRoles = await _roleService.GetAllAsync();
        model.SelectedRoleIds = await _userService.GetAssignedRoleIdsAsync(user.Id);
        model.SelectedPermissionIds = await _userService.GetAssignedPermissionIdsAsync(user.Id);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Security.Edit")]
    public async Task<IActionResult> Edit(int id, UserFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.AllRoles = await _roleService.GetAllAsync();
            model.SelectedRoleIds = await _userService.GetAssignedRoleIdsAsync(id);
            model.SelectedPermissionIds = await _userService.GetAssignedPermissionIdsAsync(id);
            return View(model);
        }

        var result = await _userService.UpdateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            model.AllRoles = await _roleService.GetAllAsync();
            model.SelectedRoleIds = await _userService.GetAssignedRoleIdsAsync(id);
            model.SelectedPermissionIds = await _userService.GetAssignedPermissionIdsAsync(id);
            return View(model);
        }

        await _userService.SetUserRolesAsync(id, model.SelectedRoleIds);
        await _userService.SetUserPermissionsAsync(id, model.SelectedPermissionIds);

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            await _userService.ChangePasswordAsync(id, model.Password);
        }

        await _activityLogService.LogAsync(User.Identity?.Name ?? "System", "Update", "Security", nameof(User), id, $"User '{model.UserName}' updated.", HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData["Success"] = "User updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var user = await _userService.GetByIdAsync(id.Value);
        if (user is null)
        {
            return NotFound();
        }

        var model = UserFormViewModel.FromEntity(user);
        model.AllRoles = await _userService.GetUserRolesAsync(user.Id);
        model.SelectedRoleIds = await _userService.GetAssignedRoleIdsAsync(user.Id);
        model.SelectedPermissionIds = await _userService.GetAssignedPermissionIdsAsync(user.Id);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var user = await _userService.GetByIdAsync(id.Value);
        if (user is null)
        {
            return NotFound();
        }

        return View(user);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [HasPermission("Security.Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userName = (await _userService.GetByIdAsync(id))?.UserName ?? string.Empty;
        var result = await _userService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            await _activityLogService.LogAsync(User.Identity?.Name ?? "System", "Delete", "Security", nameof(User), id, $"User '{userName}' deleted.", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = "User deleted.";
        }

        return RedirectToAction(nameof(Index));
    }
}