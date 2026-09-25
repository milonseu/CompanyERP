using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompanyERP.Controllers;

[Authorize]
[HasPermission("Security.View")]
public class ActivityLogController : Controller
{
    private readonly IActivityLogService _activityLogService;

    public ActivityLogController(IActivityLogService activityLogService)
    {
        _activityLogService = activityLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? module)
    {
        var logs = string.IsNullOrWhiteSpace(module) || module == "All"
            ? await _activityLogService.GetRecentAsync(200)
            : await _activityLogService.GetByModuleAsync(module, 200);

        ViewBag.Modules = await _activityLogService.GetModulesAsync();
        ViewBag.SelectedModule = module ?? "All";
        return View(logs);
    }
}