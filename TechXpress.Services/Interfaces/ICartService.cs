using TechXpress.Services.Models;

namespace TechXpress.Services.Interfaces;

public interface ICartService
{
    /// <summary>Gets the cart summary (items, subtotal, total) from a JSON cart string.</summary>
    CartSummaryDto GetSummary(string cartJson, string? promoCode = null);

    /// <summary>Adds a product to the cart with stock validation.</summary>
    (string CartJson, CartOperationResult Result) AddItem(
        string cartJson, int productId, int quantity, IEnumerable<CartItemDto> availableProducts);

    /// <summary>Updates the quantity of a cart item.</summary>
    (string CartJson, CartOperationResult Result) UpdateQuantity(string cartJson, int productId, int quantity);

    /// <summary>Removes a product from the cart.</summary>
    (string CartJson, CartOperationResult Result) RemoveItem(string cartJson, int productId);

    /// <summary>Clears the cart and returns an empty JSON cart string.</summary>
    string ClearCart();

    /// <summary>Gets the total item count in the cart.</summary>
    int GetItemCount(string cartJson);
}
