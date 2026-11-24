using TechXpress.Data.Entities;

namespace TechXpress.Services.Models;

public record OrderDto(
    int Id,
    DateTime OrderDate,
    string? UserId,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal StoreCreditUsed,
    string? PromoCode,
    OrderStatus Status,
    string? PaymentIntentId,
    string? ShippingAddress,
    IEnumerable<OrderDetailDto> OrderDetails,
    string? CustomerName = null,
    string? CustomerEmail = null);

public record OrderDetailDto(
    int Id,
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    int? VendorId,
    string? VendorStoreName,
    ShippingStatus ShippingStatus = ShippingStatus.Pending,
    string? Carrier = null,
    string? TrackingNumber = null);
