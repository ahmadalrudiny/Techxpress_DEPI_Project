using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechXpress.Services.Interfaces;

namespace TechXpress.Web.Areas.Admin.Controllers;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class ExpensesController : Controller
{
    private readonly IAdminAnalyticsService _analyticsService;

    public ExpensesController(IAdminAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year)
    {
        var model = await _analyticsService.GetDashboardAnalyticsAsync(year);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddExpense(
        string title, string category, decimal amount, DateTime expenseDate, string? notes, int? year)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _analyticsService.AddExpenseAsync(
            title, category, amount, expenseDate, notes, userId);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index), new { year });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteExpense(int id, int? year)
    {
        var result = await _analyticsService.DeleteExpenseAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index), new { year });
    }
}
