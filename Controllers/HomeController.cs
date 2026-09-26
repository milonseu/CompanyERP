using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CompanyERP.Interfaces.Services;
using CompanyERP.Models;

namespace CompanyERP.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IDashboardService _dashboard;

    public HomeController(ILogger<HomeController> logger, IDashboardService dashboard)
    {
        _logger = logger;
        _dashboard = dashboard;
    }

    public async Task<IActionResult> Index()
    {
        var companyId = await _dashboard.GetFirstCompanyIdAsync();
        var model = await _dashboard.GetDashboardAsync(companyId);
        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
