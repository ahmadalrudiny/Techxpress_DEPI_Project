namespace TechXpress.Services.Models;

public record CartItemDto(
    int ProductId,
    string Name,
    decimal Price,
    int Quantity,
    int StockQuantity,
    string? ImageUrl);

public record CartSummaryDto(
    IEnumerable<CartItemDto> Items,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    int ItemCount);

public record CartOperationResult(bool Success, string Message);

public record PromoCodeDto(string Code, decimal Percent);
