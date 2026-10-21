using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechXpress.Services.Interfaces;
using TechXpress.Web.Infrastructure;

namespace TechXpress.Web.Controllers;

[Authorize(Roles = "Customer,Vendor")]
public class WishlistController : Controller
{
    private readonly IWishlistService _wishlistService;
    private readonly ICartService _cartService;
    private readonly IProductService _productService;

    public WishlistController(
        IWishlistService wishlistService,
        ICartService cartService,
        IProductService productService)
    {
        _wishlistService = wishlistService;
        _cartService = cartService;
        _productService = productService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private string GetCartJson() => HttpContext.Session.GetString(CartSession.Key) ?? CartSession.EmptyJson;
    private void SaveCartJson(string cartJson) => HttpContext.Session.SetString(CartSession.Key, cartJson);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId;
        if (userId == null) return Challenge();

        var model = await _wishlistService.GetWishlistAsync(userId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, string? returnUrl = null)
    {
        var userId = CurrentUserId;
        if (userId == null) return Challenge();

        var (success, message) = await _wishlistService.AddAsync(userId, productId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int wishlistItemId)
    {
        var userId = CurrentUserId;
        if (userId == null) return Challenge();

        var (success, message) = await _wishlistService.RemoveAsync(userId, wishlistItemId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveProduct(int productId, string? returnUrl = null)
    {
        var userId = CurrentUserId;
        if (userId == null) return Challenge();

        var (success, message) = await _wishlistService.RemoveByProductAsync(userId, productId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(int productId)
    {
        var userId = CurrentUserId;
        if (userId == null) return Challenge();

        var product = await _productService.GetByIdAsync(productId);
        if (product == null)
        {
            TempData["ErrorMessage"] = "هذا المنتج غير متاح حالياً.";
            return RedirectToAction(nameof(Index));
        }

        var available = new[]
        {
            new TechXpress.Services.Models.CartItemDto(
                product.Id, product.Name, product.Price, 0, product.StockQuantity, product.ImageUrl)
        };

        var (newJson, result) = _cartService.AddItem(GetCartJson(), productId, 1, available);
        SaveCartJson(newJson);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Index", "Cart");
        }

        TempData["ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
