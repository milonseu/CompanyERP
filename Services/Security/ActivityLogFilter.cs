using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CompanyERP.Services.Security;

public sealed class ActivityLogFilter : IAsyncActionFilter
{
    private static readonly HashSet<string> WriteVerbs =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    private static readonly HashSet<string> ExcludedControllers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Account", "Home", "User", "Role", "Permission", "Menu",
            "ActivityLog", "LoginHistory"
        };

    private readonly IActivityLogService _activityLog;
    private readonly ILogger<ActivityLogFilter> _logger;

    public ActivityLogFilter(IActivityLogService activityLog, ILogger<ActivityLogFilter> logger)
    {
        _activityLog = activityLog;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();

        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (!WriteVerbs.Contains(context.HttpContext.Request.Method))
        {
            return;
        }

        var controller = context.RouteData.Values["controller"]?.ToString() ?? string.Empty;
        if (ExcludedControllers.Contains(controller))
        {
            return;
        }

        if (executed.Exception is not null)
        {
            return;
        }

        if (executed.Result is BadRequestObjectResult or NotFoundResult or StatusCodeResult)
        {
            return;
        }

        var module = SecurityDefs.GetModule(controller);
        var action = context.RouteData.Values["action"]?.ToString() ?? string.Empty;
        var userName = context.HttpContext.User.Identity?.Name ?? "System";

        try
        {
            await _activityLog.LogAsync(
                userName,
                context.HttpContext.Request.Method,
                string.IsNullOrEmpty(module) ? controller : module,
                controller,
                null,
                $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}",
                context.HttpContext.Connection.RemoteIpAddress?.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write business activity log.");
        }
    }
}