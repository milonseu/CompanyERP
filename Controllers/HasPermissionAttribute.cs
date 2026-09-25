using System.Security.Claims;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CompanyERP.Controllers;

public sealed class HasPermissionAttribute : TypeFilterAttribute
{
    public HasPermissionAttribute(string permissionCode)
        : base(typeof(HasPermissionFilter))
    {
        Arguments = [permissionCode];
    }

    private sealed class HasPermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly string _permissionCode;
        private readonly IAuthService _authService;

        public HasPermissionFilter(string permissionCode, IAuthService authService)
        {
            _permissionCode = permissionCode;
            _authService = authService;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var userIdValue = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
            {
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = context.HttpContext.Request.Path });
                return;
            }

            if (!await _authService.HasPermissionAsync(userId, _permissionCode))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            }
        }
    }
}