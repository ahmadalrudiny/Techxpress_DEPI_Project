using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;
using TechXpress.Web.Infrastructure;

namespace TechXpress.Web.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cartService;
    private readonly ICartPersistenceService _cartPersistence;
    private readonly IProductService _productService;

    public CartController(
        ICartService cartService,
        ICartPersistenceService cartPersistence,
        IProductService productService)
    {
        _cartService = cartService;
        _cartPersistence = cartPersistence;
        _productService = productService;
    }

    private string? CurrentUserId =>
        User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

    /// <summary>
    /// يقرأ السلة من الجلسة، وإن كانت فارغة يستعيد النسخة المحفوظة للمستخدم من قاعدة البيانات.
    /// </summary>
    private async Task<string> GetCartJsonAsync()
    {
        var cartJson = HttpContext.Session.GetString(CartSession.Key);
        if (!string.IsNullOrWhiteSpace(cartJson) && cartJson != CartSession.EmptyJson)
        {
            return cartJson;
        }

        var userId = CurrentUserId;
        if (userId is null)
        {
            return cartJson ?? CartSession.EmptyJson;
        }

        var saved = await _cartPersistence.GetSavedCartAsync(userId);
        if (!string.IsNullOrWhiteSpace(saved))
        {
            HttpContext.Session.SetString(CartSession.Key, saved);
            return saved;
        }

        return cartJson ?? CartSession.EmptyJson;
    }

    private async Task SaveCartJsonAsync(string cartJson)
    {
        HttpContext.Session.SetString(CartSession.Key, cartJson);

        var userId = CurrentUserId;
        if (userId is not null)
        {
            await _cartPersistence.SaveCartAsync(userId, cartJson);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? promoCode = null)
    {
        var cartJson = await GetCartJsonAsync();
        var summary = _cartService.GetSummary(cartJson, promoCode);
        return View(summary);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int id, int quantity = 1)
    {
        var cartJson = await GetCartJsonAsync();
        var product = await _productService.GetByIdAsync(id);
        if (product == null)
            return Json(new { success = false, message = "المنتج غير موجود." });

        var available = new[]
        {
            new CartItemDto(product.Id, product.Name, product.Price, 0, product.StockQuantity, product.ImageUrl)
        };

        var (newJson, result) = _cartService.AddItem(cartJson, id, quantity, available);
        await SaveCartJsonAsync(newJson);

        return Json(new
        {
            success = result.Success,
            message = result.Message,
            cartCount = _cartService.GetItemCount(newJson)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, int quantity)
    {
        var cartJson = await GetCartJsonAsync();
        var (newJson, result) = _cartService.UpdateQuantity(cartJson, id, quantity);
        await SaveCartJsonAsync(newJson);

        return Json(new
        {
            success = result.Success,
            message = result.Message,
            cartCount = _cartService.GetItemCount(newJson)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id)
    {
        var cartJson = await GetCartJsonAsync();
        var (newJson, result) = _cartService.RemoveItem(cartJson, id);
        await SaveCartJsonAsync(newJson);

        return Json(new
        {
            success = result.Success,
            message = result.Message,
            cartCount = _cartService.GetItemCount(newJson)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        var newJson = _cartService.ClearCart();
        HttpContext.Session.SetString(CartSession.Key, newJson);

        var userId = CurrentUserId;
        if (userId is not null)
        {
            await _cartPersistence.ClearAsync(userId);
        }

        return Json(new
        {
            success = true,
            message = "تم تفريغ السلة.",
            cartCount = 0
        });
    }

    [HttpGet]
    public async Task<IActionResult> Count()
    {
        var cartJson = await GetCartJsonAsync();
        var count = _cartService.GetItemCount(cartJson);
        return Json(new { cartCount = count });
    }
}
