using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechXpress.Services.Interfaces;

namespace TechXpress.Web.Controllers;

[Authorize(Roles = "Vendor")]
public class VendorOrderController : Controller
{
    private readonly IVendorService _vendorService;

    public VendorOrderController(IVendorService vendorService)
    {
        _vendorService = vendorService;
    }

    private async Task<int?> GetCurrentVendorIdAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return null;
        return await _vendorService.GetVendorIdByUserIdAsync(userId);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vendorId = await GetCurrentVendorIdAsync();
        if (vendorId == null) return Forbid();

        var model = await _vendorService.GetVendorOrdersAsync(vendorId.Value);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var vendorId = await GetCurrentVendorIdAsync();
        if (vendorId == null) return Forbid();

        var model = await _vendorService.GetVendorOrderAsync(vendorId.Value, id);
        if (model == null) return NotFound();

        return View(model);
    }

    // ===== الشحن (مسؤولية البائع) =====

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartPreparing(int id)
    {
        var vendorId = await GetCurrentVendorIdAsync();
        if (vendorId == null) return Forbid();

        var (success, message) = await _vendorService.StartPreparingAsync(vendorId.Value, id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ship(int id, string carrier, string? trackingNumber)
    {
        var vendorId = await GetCurrentVendorIdAsync();
        if (vendorId == null) return Forbid();

        var (success, message) = await _vendorService.ShipOrderAsync(vendorId.Value, id, carrier, trackingNumber);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deliver(int id)
    {
        var vendorId = await GetCurrentVendorIdAsync();
        if (vendorId == null) return Forbid();

        var (success, message) = await _vendorService.MarkDeliveredAsync(vendorId.Value, id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }
}
