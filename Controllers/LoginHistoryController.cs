using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompanyERP.Controllers;

[Authorize]
[HasPermission("Security.View")]
public class LoginHistoryController : Controller
{
    private readonly ILoginHistoryService _loginHistoryService;

    public LoginHistoryController(ILoginHistoryService loginHistoryService)
    {
        _loginHistoryService = loginHistoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var history = await _loginHistoryService.GetRecentAsync(200);
        return View(history);
    }
}