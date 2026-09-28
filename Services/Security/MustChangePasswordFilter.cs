using System.Security.Claims;
using CompanyERP.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CompanyERP.Services.Security;

/// <summary>
/// Confines a signed-in user to the change-password screen while their account still carries a
/// password nobody chose (the seeded default administrator). Without this the marker set at
/// sign-in would be advisory only, and the well-known credential could keep working unnoticed.
/// </summary>
public sealed class MustChangePasswordFilter : IAsyncActionFilter
{
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            return next();
        }

        var marker = context.HttpContext.User.FindFirstValue(AccountController.PasswordChangeClaim);
        if (marker != "1")
        {
            return next();
        }

        var action = context.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
        var controllerName = action?.ControllerName;
        var actionName = action?.ActionName;

        var isPasswordChange = string.Equals(controllerName, "Account", StringComparison.OrdinalIgnoreCase)
            && string.Equals(actionName, "ChangePassword", StringComparison.OrdinalIgnoreCase);

        var isLogout = string.Equals(controllerName, "Account", StringComparison.OrdinalIgnoreCase)
            && string.Equals(actionName, "Logout", StringComparison.OrdinalIgnoreCase);

        if (isPasswordChange || isLogout)
        {
            return next();
        }

        context.Result = new RedirectToActionResult("ChangePassword", "Account", null);
        return Task.CompletedTask;
    }
}
