using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechXpress.Data.Entities;
using TechXpress.Services.Interfaces;

namespace TechXpress.Web.Areas.Admin.Controllers;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var orders = await _orderService.GetAllOrdersAsync();
        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _orderService.GetOrderAsync(id, null);
        if (order == null) return NotFound();
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string newStatus)
    {
        if (!Enum.TryParse<OrderStatus>(newStatus, out var status))
        {
            if (IsAjaxRequest) return Json(new { success = false, message = "حالة الطلب غير صحيحة." });
            return BadRequest("Invalid status");
        }

        var (success, message) = await _orderService.UpdateStatusAsync(id, status);

        if (IsAjaxRequest) return Json(new { success, message });

        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    private bool IsAjaxRequest => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        // Passing null user id means "administrator action": the ownership check is skipped.
        var (success, message) = await _orderService.CancelOrderAsync(id, null);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(int id)
    {
        // Administrator return of a delivered order; the value is refunded as store credit.
        var (success, message) = await _orderService.ReturnOrderAsync(id, null);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }
}
