using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;
using TechXpress.Web.Models;

namespace TechXpress.Web.Controllers;

[Authorize(Roles = "Customer,Vendor")]
public class ReviewController : Controller
{
    private readonly IReviewService _reviewService;

    public ReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReviewViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "التقييم غير صالح: تأكد من اختيار النجوم وكتابة تعليق صحيح.";
            return RedirectToAction("Details", "Products", new { id = model.ProductId });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Challenge();

        var (success, message) = await _reviewService.CreateReviewAsync(
            userId, new CreateReviewRequest(model.ProductId, model.Rating, model.Comment));

        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction("Details", "Products", new { id = model.ProductId });
    }
}
