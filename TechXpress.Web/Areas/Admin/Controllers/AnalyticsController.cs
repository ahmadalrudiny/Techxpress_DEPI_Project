using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechXpress.Services.Interfaces;

namespace TechXpress.Web.Areas.Admin.Controllers;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class AnalyticsController : Controller
{
    private readonly IAdminAnalyticsService _analyticsService;

    public AnalyticsController(IAdminAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year)
    {
        var model = await _analyticsService.GetDashboardAnalyticsAsync(year);
        return View(model);
    }
}
