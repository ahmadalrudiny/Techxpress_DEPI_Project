using TechXpress.Data.Entities;
using TechXpress.Services.Models;

namespace TechXpress.Services.Interfaces;

public interface IOrderService
{
    /// <summary>
    /// Places an order from the cart in one transaction: re-checks stock from the database,
    /// reduces stock, creates Order (Status Pending) and OrderDetails with UnitPrice from the database,
    /// sets OrderDetail.VendorId from Product.VendorId, applies discount and stores DiscountAmount/PromoCode.
    /// </summary>
    Task<(bool Success, string Message, int? OrderId)> PlaceOrderAsync(
        string userId, string cartJson, string shippingAddress, string? promoCode = null);

    /// <summary>Gets all orders for a specific user (customer sees only own orders).</summary>
    Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId);

    /// <summary>Gets an order by ID, optionally enforcing ownership for a specific user (pass <c>null</c> for admin access).</summary>
    Task<OrderDto?> GetOrderAsync(int id, string? userId);

    /// <summary>Gets all orders with users (admin).</summary>
    Task<IEnumerable<OrderDto>> GetAllOrdersAsync();

    /// <summary>
    /// Updates order status with allowed transitions only:
    /// Pending->Paid/Cancelled, Paid->Shipped/Cancelled, Shipped->Delivered.
    /// </summary>
    Task<(bool Success, string Message)> UpdateStatusAsync(int id, OrderStatus newStatus);

    /// <summary>
    /// Cancels an order, restores stock in the same transaction, and if the order was Paid,
    /// calls ReverseOrderCommissionAsync to deduct vendor earnings.
    /// </summary>
    /// <summary>
    /// Cancels an order, restores the reserved stock and reverses any vendor commission when the
    /// order had already been paid.
    /// </summary>
    /// <param name="id">Identifier of the order to cancel.</param>
    /// <param name="userId">
    /// Identifier of the customer that owns the order. Pass <c>null</c> when the action is performed
    /// by an administrator, which bypasses the ownership check.
    /// </param>
    /// <returns>A tuple with the success flag and a user-facing message.</returns>
    Task<(bool Success, string Message)> CancelOrderAsync(int id, string? userId);
}
