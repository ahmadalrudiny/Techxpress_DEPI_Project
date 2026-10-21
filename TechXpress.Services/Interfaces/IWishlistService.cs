using TechXpress.Services.Models;

namespace TechXpress.Services.Interfaces;

public interface IWishlistService
{
    /// <summary>Gets the wishlist of a user with availability information.</summary>
    Task<WishlistDto> GetWishlistAsync(string userId);

    /// <summary>Adds a product to the user's wishlist (no duplicates).</summary>
    Task<(bool Success, string Message)> AddAsync(string userId, int productId);

    /// <summary>Removes a wishlist item by its ID (ownership checked).</summary>
    Task<(bool Success, string Message)> RemoveAsync(string userId, int wishlistItemId);

    /// <summary>Removes a product from the user's wishlist by product ID.</summary>
    Task<(bool Success, string Message)> RemoveByProductAsync(string userId, int productId);

    /// <summary>Returns true when the product is already in the user's wishlist.</summary>
    Task<bool> IsInWishlistAsync(string userId, int productId);

    /// <summary>Number of items in the user's wishlist.</summary>
    Task<int> GetWishlistCountAsync(string userId);
}
