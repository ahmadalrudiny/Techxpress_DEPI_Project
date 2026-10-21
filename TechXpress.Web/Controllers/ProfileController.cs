using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechXpress.Data.Entities;
using TechXpress.Services.Interfaces;
using TechXpress.Web.Models;

namespace TechXpress.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOrderService _orderService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IOrderService orderService,
        ILogger<ProfileController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _orderService = orderService;
        _logger = logger;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var model = new ProfileViewModel
        {
            FullName = user.FullName,
            Address = user.Address ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Email = user.Email ?? string.Empty,
            City = user.City ?? string.Empty,
            PostalCode = user.PostalCode ?? string.Empty
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var model = new EditProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Address = user.Address ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            City = user.City ?? string.Empty,
            PostalCode = user.PostalCode ?? string.Empty
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditProfileViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        user.FullName = model.FullName;
        user.Address = model.Address;
        user.PhoneNumber = model.PhoneNumber;
        user.City = model.City;
        user.PostalCode = model.PostalCode;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        _logger.LogInformation("User {UserId} updated profile", user.Id);
        TempData["SuccessMessage"] = "تم تحديث الملف الشخصي بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult ChangePassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        _logger.LogInformation("User {UserId} changed password", user.Id);
        TempData["SuccessMessage"] = "تم تغيير كلمة المرور بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Orders()
    {
        var userId = CurrentUserId;
        if (userId == null) return Challenge();

        var orders = await _orderService.GetUserOrdersAsync(userId);
        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> OrderDetails(int id)
    {
        var userId = CurrentUserId;
        if (userId == null) return Challenge();

        // الخدمة تتحقق من أن الطلب يخص هذا المستخدم
        var order = await _orderService.GetOrderAsync(id, userId);
        if (order == null) return NotFound();

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var userId = CurrentUserId;
        if (userId == null) return Challenge();

        var (success, message) = await _orderService.CancelOrderAsync(id, userId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Orders));
    }
}
