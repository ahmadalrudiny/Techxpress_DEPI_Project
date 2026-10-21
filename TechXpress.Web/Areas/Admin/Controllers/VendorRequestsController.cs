using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechXpress.Data.Entities;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;

namespace TechXpress.Web.Areas.Admin.Controllers;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class VendorRequestsController : Controller
{
    private readonly IVendorService _vendorService;
    private readonly IVendorDocumentStorage _documentStorage;

    public VendorRequestsController(IVendorService vendorService, IVendorDocumentStorage documentStorage)
    {
        _vendorService = vendorService;
        _documentStorage = documentStorage;
    }

    [HttpGet]
    public async Task<IActionResult> Index(VendorStatus? status = null)
    {
        var requests = await _vendorService.GetRequestsAsync(status);
        ViewData["Status"] = status?.ToString();
        return View(requests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin";
        var (success, message) = await _vendorService.ApproveAsync(id, adminUserId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin";
        var (success, message) = await _vendorService.RejectAsync(id, adminUserId, reason);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Document(int vendorId, VendorDocumentType type)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin";
        var document = await _vendorService.OpenDocumentAsync(vendorId, type, adminUserId);
        if (document == null) return NotFound();

        return File(document.Content, document.ContentType);
    }
}
