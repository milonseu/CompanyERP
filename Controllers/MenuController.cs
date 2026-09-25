using CompanyERP.Entities.Security;
using CompanyERP.Interfaces.Services;
using CompanyERP.ViewModels.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompanyERP.Controllers;

[Authorize]
[HasPermission("Security.View")]
public class MenuController : Controller
{
    private readonly IMenuService _menuService;
    private readonly IActivityLogService _activityLogService;

    public MenuController(IMenuService menuService, IActivityLogService activityLogService)
    {
        _menuService = menuService;
        _activityLogService = activityLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var menus = await _menuService.GetAllAsync();
        return View(menus);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new MenuFormViewModel
        {
            AllMenus = await _menuService.GetTopLevelAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Security.Create")]
    public async Task<IActionResult> Create(MenuFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AllMenus = await _menuService.GetTopLevelAsync();
            return View(model);
        }

        var result = await _menuService.CreateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            model.AllMenus = await _menuService.GetTopLevelAsync();
            return View(model);
        }

        await _activityLogService.LogAsync(User.Identity?.Name ?? "System", "Create", "Security", nameof(Menu), null, $"Menu '{model.Code}' created.", HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData["Success"] = "Menu created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var menu = await _menuService.GetByIdAsync(id.Value);
        if (menu is null)
        {
            return NotFound();
        }

        var model = MenuFormViewModel.FromEntity(menu);
        model.AllMenus = await _menuService.GetTopLevelAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Security.Edit")]
    public async Task<IActionResult> Edit(int id, MenuFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.AllMenus = await _menuService.GetTopLevelAsync();
            return View(model);
        }

        var result = await _menuService.UpdateAsync(model.ToEntity());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            model.AllMenus = await _menuService.GetTopLevelAsync();
            return View(model);
        }

        await _activityLogService.LogAsync(User.Identity?.Name ?? "System", "Update", "Security", nameof(Menu), id, $"Menu '{model.Code}' updated.", HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData["Success"] = "Menu updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var menu = await _menuService.GetByIdAsync(id.Value);
        if (menu is null)
        {
            return NotFound();
        }

        ViewBag.HasChildren = await _menuService.HasChildrenAsync(id.Value);
        return View(menu);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [HasPermission("Security.Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var code = (await _menuService.GetByIdAsync(id))?.Code ?? string.Empty;
        var result = await _menuService.DeleteAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            await _activityLogService.LogAsync(User.Identity?.Name ?? "System", "Delete", "Security", nameof(Menu), id, $"Menu '{code}' deleted.", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = "Menu deleted.";
        }

        return RedirectToAction(nameof(Index));
    }
}