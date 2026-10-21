using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;

namespace TechXpress.Services.Implementations;

public class CartService : ICartService
{
    private readonly ILogger<CartService> _logger;
    private readonly List<PromoCodeDto> _promoCodes;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public CartService(ILogger<CartService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _promoCodes = configuration.GetSection("Promotions").Get<List<PromoCodeDto>>() ?? new();
    }

    public CartSummaryDto GetSummary(string cartJson, string? promoCode = null)
    {
        var items = Deserialize(cartJson);
        var subtotal = items.Sum(i => i.Price * i.Quantity);
        var itemCount = items.Sum(i => i.Quantity);
        var discount = 0m;

        if (!string.IsNullOrWhiteSpace(promoCode))
        {
            var (promoDiscount, _) = ApplyPromoInternal(items, promoCode.Trim());
            discount = promoDiscount;
        }

        var total = subtotal - discount;
        return new CartSummaryDto(items, subtotal, discount, total, itemCount);
    }

    public (string CartJson, CartOperationResult Result) AddItem(
        string cartJson, int productId, int quantity, IEnumerable<CartItemDto> availableProducts)
    {
        if (quantity <= 0)
            return (cartJson, new CartOperationResult(false, "الكمية يجب أن تكون أكبر من صفر."));

        var product = availableProducts.FirstOrDefault(p => p.ProductId == productId);
        if (product == null)
            return (cartJson, new CartOperationResult(false, "المنتج غير موجود."));

        var items = Deserialize(cartJson).ToList();
        var existing = items.FirstOrDefault(i => i.ProductId == productId);
        var newQuantity = (existing?.Quantity ?? 0) + quantity;

        if (newQuantity > product.StockQuantity)
            return (cartJson, new CartOperationResult(false, $"الكمية المتاحة في المخزون {product.StockQuantity} فقط."));

        if (existing != null)
        {
            items.Remove(existing);
            items.Add(existing with { Quantity = newQuantity });
        }
        else
        {
            items.Add(product with { Quantity = quantity });
        }

        var newJson = Serialize(items);
        _logger.LogInformation("Added product {ProductId} (qty {Quantity}) to cart", productId, quantity);
        return (newJson, new CartOperationResult(true, "تمت إضافة المنتج إلى السلة."));
    }

    public (string CartJson, CartOperationResult Result) UpdateQuantity(string cartJson, int productId, int quantity)
    {
        if (quantity < 0)
            return (cartJson, new CartOperationResult(false, "الكمية لا يمكن أن تكون سالبة."));

        var items = Deserialize(cartJson).ToList();
        var existing = items.FirstOrDefault(i => i.ProductId == productId);
        if (existing == null)
            return (cartJson, new CartOperationResult(false, "العنصر غير موجود في السلة."));

        if (quantity == 0)
        {
            items.Remove(existing);
        }
        else
        {
            if (quantity > existing.StockQuantity)
                return (cartJson, new CartOperationResult(false, $"الكمية المتاحة في المخزون {existing.StockQuantity} فقط."));
            items.Remove(existing);
            items.Add(existing with { Quantity = quantity });
        }

        var newJson = Serialize(items);
        _logger.LogInformation("Updated product {ProductId} quantity to {Quantity} in cart", productId, quantity);
        return (newJson, new CartOperationResult(true, "تم تحديث السلة."));
    }

    public (string CartJson, CartOperationResult Result) RemoveItem(string cartJson, int productId)
    {
        var items = Deserialize(cartJson).ToList();
        var existing = items.FirstOrDefault(i => i.ProductId == productId);
        if (existing == null)
            return (cartJson, new CartOperationResult(false, "العنصر غير موجود في السلة."));

        items.Remove(existing);
        var newJson = Serialize(items);
        _logger.LogInformation("Removed product {ProductId} from cart", productId);
        return (newJson, new CartOperationResult(true, "تم حذف العنصر من السلة."));
    }

    public string ClearCart()
    {
        _logger.LogInformation("Cart cleared");
        return "[]";
    }

    public int GetItemCount(string cartJson)
    {
        var items = Deserialize(cartJson);
        return items.Sum(i => i.Quantity);
    }

    private (decimal Discount, string Message) ApplyPromoInternal(List<CartItemDto> items, string promoCode)
    {
        var promo = _promoCodes.FirstOrDefault(p => p.Code.Equals(promoCode, StringComparison.OrdinalIgnoreCase));
        if (promo == null)
            return (0m, "Invalid promo code.");

        var subtotal = items.Sum(i => i.Price * i.Quantity);
        var discount = Math.Round(subtotal * promo.Percent / 100m, 2);
        return (discount, $"Promo code '{promo.Code}' applied: {promo.Percent}% off.");
    }

    private List<CartItemDto> Deserialize(string cartJson)
    {
        if (string.IsNullOrWhiteSpace(cartJson))
            return new();
        return JsonSerializer.Deserialize<List<CartItemDto>>(cartJson, JsonOptions) ?? new();
    }

    private string Serialize(List<CartItemDto> items)
    {
        return JsonSerializer.Serialize(items, JsonOptions);
    }
}
