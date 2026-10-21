using Microsoft.Extensions.Logging;
using TechXpress.Data.Entities;
using TechXpress.Data.UoW;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;

namespace TechXpress.Services.Implementations;

public class WishlistService : IWishlistService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WishlistService> _logger;

    public WishlistService(IUnitOfWork unitOfWork, ILogger<WishlistService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<WishlistDto> GetWishlistAsync(string userId)
    {
        var items = await _unitOfWork.Wishlists.GetByUserIdAsync(userId);

        var dtos = items.Select(w => new WishlistItemDto(
            w.Id,
            w.ProductId,
            w.Product?.Name ?? "منتج غير متاح",
            w.Product?.ImageUrl,
            w.Product?.Category?.Name ?? "غير مصنّف",
            w.Product?.Price ?? 0m,
            w.Product?.StockQuantity ?? 0,
            w.DateAdded,
            w.Product != null && w.Product.IsActive && w.Product.StockQuantity > 0));

        return new WishlistDto(dtos);
    }

    public async Task<(bool Success, string Message)> AddAsync(string userId, int productId)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null || !product.IsActive)
            return (false, "هذا المنتج غير متاح.");

        var existing = await _unitOfWork.Wishlists.GetItemAsync(userId, productId);
        if (existing != null)
            return (false, $"المنتج «{product.Name}» موجود بالفعل في مفضلاتك.");

        await _unitOfWork.Wishlists.AddAsync(new WishlistItem
        {
            UserId = userId,
            ProductId = productId,
            DateAdded = DateTime.UtcNow
        });
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User {UserId} added product {ProductId} to wishlist", userId, productId);
        return (true, $"تمت إضافة «{product.Name}» إلى مفضلاتك.");
    }

    public async Task<(bool Success, string Message)> RemoveAsync(string userId, int wishlistItemId)
    {
        var item = await _unitOfWork.Wishlists.GetByIdForUserAsync(wishlistItemId, userId);
        if (item == null)
            return (false, "العنصر غير موجود في مفضلاتك.");

        var name = item.Product?.Name ?? "المنتج";
        _unitOfWork.Wishlists.Delete(item);
        await _unitOfWork.SaveChangesAsync();

        return (true, $"تمت إزالة «{name}» من مفضلاتك.");
    }

    public async Task<(bool Success, string Message)> RemoveByProductAsync(string userId, int productId)
    {
        var item = await _unitOfWork.Wishlists.GetItemAsync(userId, productId);
        if (item == null)
            return (false, "المنتج غير موجود في مفضلاتك.");

        var name = item.Product?.Name ?? "المنتج";
        _unitOfWork.Wishlists.Delete(item);
        await _unitOfWork.SaveChangesAsync();

        return (true, $"تمت إزالة «{name}» من مفضلاتك.");
    }

    public async Task<bool> IsInWishlistAsync(string userId, int productId)
    {
        var item = await _unitOfWork.Wishlists.GetItemAsync(userId, productId);
        return item != null;
    }

    public async Task<int> GetWishlistCountAsync(string userId)
    {
        var items = await _unitOfWork.Wishlists.GetByUserIdAsync(userId);
        return items.Count;
    }
}
