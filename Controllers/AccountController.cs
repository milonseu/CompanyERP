using System.Security.Claims;
using CompanyERP.Entities.Security;
using CompanyERP.Interfaces.Services;
using CompanyERP.Services.Security;
using CompanyERP.ViewModels.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompanyERP.Controllers;

public class AccountController : Controller
{
    /// <summary>Claim carrying the "still using a seeded password" marker set at sign-in.</summary>
    public const string PasswordChangeClaim = "MustChangePassword";

    private readonly IAuthService _authService;
    private readonly IUserService _userService;

    public AccountController(IAuthService authService, IUserService userService)
    {
        _authService = authService;
        _userService = userService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _authService.ValidateAsync(model.UserName, model.Password);
        if (!result.Success)
        {
            await _authService.RecordLoginFailAsync(model.UserName, ip, result.Error);
            TempData["Error"] = result.Error;
            return View(model);
        }

        var user = result.User!;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new("FullName", user.FullName ?? user.UserName),
            new(PasswordChangeClaim, user.MustChangePassword ? "1" : "0")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = model.RememberMe });

        await _authService.RecordLoginSuccessAsync(user.Id, ip);

        if (user.MustChangePassword)
        {
            return RedirectToAction(nameof(ChangePassword));
        }

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdValue, out var userId))
        {
            await _authService.RecordLogoutAsync(userId);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        if (await _userService.AnyUserExistsAsync())
        {
            return RedirectToAction("Login", "Account");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (await _userService.AnyUserExistsAsync())
        {
            TempData["Error"] = "Registration is closed. The system already has a user account.";
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _userService.UserNameExistsAsync(model.UserName))
        {
            ModelState.AddModelError(nameof(model.UserName), "That user name is already taken.");
            return View(model);
        }

        var user = new User
        {
            UserName = model.UserName,
            FullName = model.FullName,
            Email = model.Email,
            IsSystem = false
        };

        var createResult = await _userService.CreateFirstSuperAdminAsync(user, model.Password);
        if (!createResult.Success)
        {
            TempData["Error"] = createResult.Error;
            return View(model);
        }

        TempData["Success"] = "Registration successful. You are the Super Admin with full system access. Please log in.";
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _userService.ChangePasswordAsync(userId, model.NewPassword);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        // Re-issue the cookie so the "must change" marker is dropped, otherwise the old
        // authentication ticket would keep the user locked on this screen.
        var user = await _userService.GetByIdAsync(userId);
        if (user is not null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName),
                new("FullName", user.FullName ?? user.UserName)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
                new AuthenticationProperties { IsPersistent = false });
        }

        TempData["Success"] = "Password updated.";
        return RedirectToAction("Index", "Home");
    }
}