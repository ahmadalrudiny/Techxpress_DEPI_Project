using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechXpress.Services.Interfaces;

namespace TechXpress.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly IUserManagementService _userManagementService;

    public UsersController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userManagementService.GetAllUsersAsync();
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(string userId)
    {
        var (success, message) = await _userManagementService.ActivateUserAsync(userId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(string userId)
    {
        // هوية الأدمن الحالي تُقرأ من الـ Claims وليس من النموذج
        var currentAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        var (success, message) = await _userManagementService
            .SuspendUserAsync(userId, currentAdminId);

        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Index));
    }
}
