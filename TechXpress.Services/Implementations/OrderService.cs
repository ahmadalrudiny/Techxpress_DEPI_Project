using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechXpress.Data.Entities;
using TechXpress.Data.UoW;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;

namespace TechXpress.Services.Implementations;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;
    private readonly ICartService _cartService;
    private readonly IWalletService _walletService;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OrderService(IUnitOfWork unitOfWork, ILogger<OrderService> logger,
        ICartService cartService, IWalletService walletService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cartService = cartService;
        _walletService = walletService;
    }

    public async Task<(bool Success, string Message, int? OrderId)> PlaceOrderAsync(
        string userId, string cartJson, string shippingAddress, string? promoCode = null)
    {
        if (string.IsNullOrWhiteSpace(shippingAddress))
            return (false, "عنوان الشحن مطلوب.", null);

        var cartItems = _cartService.GetSummary(cartJson).Items.ToList();
        if (!cartItems.Any())
            return (false, "سلة الشراء فارغة.", null);

        // Validate promo code
        decimal discountAmount = 0;
        string? appliedPromoCode = null;
        if (!string.IsNullOrWhiteSpace(promoCode))
        {
            var promoSummary = _cartService.GetSummary(cartJson, promoCode);
            if (promoSummary.Discount > 0)
            {
                discountAmount = promoSummary.Discount;
                appliedPromoCode = promoCode.Trim();
            }
            else
            {
                return (false, "كود الخصم غير صالح.", null);
            }
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // 1. Re-check stock from the database and reduce it
            foreach (var item in cartItems)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return (false, $"المنتج {item.Name} لم يعد موجودًا.", null);
                }
                if (product.StockQuantity < item.Quantity)
                {
                    await _unitOfWork.RollbackAsync();
                    return (false, $"الكمية المتاحة من {item.Name} غير كافية. المتاح: {product.StockQuantity}.", null);
                }

                product.StockQuantity -= item.Quantity;
                _unitOfWork.Products.Update(product);
            }

            // 2. Calculate totals
            var subtotal = cartItems.Sum(i => i.Price * i.Quantity);
            var totalAmount = subtotal - discountAmount;

            // 3. Create Order
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = totalAmount,
                DiscountAmount = discountAmount,
                PromoCode = appliedPromoCode,
                Status = OrderStatus.Pending,
                ShippingAddress = shippingAddress
            };

            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveChangesAsync(); // to get order.Id

            // 4. Create OrderDetails with UnitPrice from database, VendorId from Product.VendorId
            foreach (var item in cartItems)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return (false, $"المنتج {item.Name} لم يعد متاحًا.", null);
                }

                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price, // from database, never from client
                    VendorId = product.VendorId
                };
                await _unitOfWork.OrderDetails.AddAsync(orderDetail);
            }

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Order {OrderId} placed for user {UserId}", order.Id, userId);
            return (true, "تم إنشاء طلبك بنجاح.", order.Id);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Failed to place order for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId)
    {
        var orders = await _unitOfWork.Orders.GetByUserIdAsync(userId);
        return MapToDtos(orders);
    }

    public async Task<OrderDto?> GetOrderAsync(int id, string? userId)
    {
        var order = userId == null
            ? await _unitOfWork.Orders.GetByIdWithDetailsAsync(id)
            : await _unitOfWork.Orders.GetByIdForUserAsync(id, userId);

        if (order == null) return null;
        return MapToDto(order);
    }

    public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync()
    {
        var orders = await _unitOfWork.Orders.GetAllWithUsersAsync();
        return MapToDtos(orders);
    }

    public async Task<(bool Success, string Message)> UpdateStatusAsync(int id, OrderStatus newStatus)
    {
        var order = await _unitOfWork.Orders.GetByIdWithDetailsAsync(id);
        if (order == null)
            return (false, "الطلب غير موجود.");

        // Allowed transitions
        bool allowed = (order.Status, newStatus) switch
        {
            (OrderStatus.Pending, OrderStatus.Paid) => true,
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,
            (OrderStatus.Paid, OrderStatus.Shipped) => true,
            (OrderStatus.Paid, OrderStatus.Cancelled) => true,
            (OrderStatus.Shipped, OrderStatus.Delivered) => true,
            _ => false
        };

        if (!allowed)
            return (false, "حالة الطلب غير صحيحة.");

        order.Status = newStatus;

        // مزامنة حالة الشحن على مستوى البنود عند تدخّل الأدمن يدويًا
        var now = DateTime.UtcNow;
        if (newStatus == OrderStatus.Shipped)
        {
            foreach (var detail in order.OrderDetails
                         .Where(d => d.ShippingStatus is ShippingStatus.Pending or ShippingStatus.Preparing))
            {
                detail.ShippingStatus = ShippingStatus.Shipped;
                detail.ShippedAt = now;
            }
        }
        else if (newStatus == OrderStatus.Delivered)
        {
            foreach (var detail in order.OrderDetails
                         .Where(d => d.ShippingStatus != ShippingStatus.Delivered))
            {
                detail.ShippingStatus = ShippingStatus.Delivered;
                detail.DeliveredAt = now;
            }
        }

        _unitOfWork.Orders.Update(order);

        try
        {
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Order {OrderId} status changed to {Status}", id, newStatus);
            return (true, "تم تحديث حالة الطلب بنجاح.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update order {OrderId} status", id);
            throw;
        }
    }

    public async Task<(bool Success, string Message)> CancelOrderAsync(int id, string? userId)
    {
        var order = userId == null
            ? await _unitOfWork.Orders.GetByIdWithDetailsAsync(id)
            : await _unitOfWork.Orders.GetByIdForUserAsync(id, userId);
        if (order == null)
            return (false, "الطلب غير موجود أو لا تملك صلاحية الوصول إليه.");

        if (order.Status == OrderStatus.Cancelled)
            return (false, "تم إلغاء هذا الطلب بالفعل.");

        if (order.Status == OrderStatus.Delivered)
            return (false, "لا يمكن إلغاء طلب تم توصيله.");

        var wasPaid = order.Status == OrderStatus.Paid;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // Restore stock
            var details = await _unitOfWork.OrderDetails
                .FindAsync(d => d.OrderId == id);

            foreach (var detail in details)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(detail.ProductId);
                if (product != null)
                {
                    product.StockQuantity += detail.Quantity;
                    _unitOfWork.Products.Update(product);
                }
            }

            order.Status = OrderStatus.Cancelled;
            _unitOfWork.Orders.Update(order);

            await _unitOfWork.CommitAsync();

            // If order was Paid, reverse commission (outside the order transaction)
            if (wasPaid)
            {
                await _walletService.ReverseOrderCommissionAsync(id);
                _logger.LogInformation("Commission reversed for cancelled order {OrderId}", id);
            }

            _logger.LogInformation("Order {OrderId} cancelled by user {UserId}", id, userId);
            return (true, "تم إلغاء الطلب واستعادة الكميات إلى المخزون.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Failed to cancel order {OrderId}", id);
            throw;
        }
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto(
            order.Id,
            order.OrderDate,
            order.UserId,
            order.TotalAmount,
            order.DiscountAmount,
            order.PromoCode,
            order.Status,
            order.PaymentIntentId,
            order.ShippingAddress,
            order.OrderDetails.Select(d => new OrderDetailDto(
                d.Id,
                d.ProductId,
                d.Product?.Name ?? string.Empty,
                d.Quantity,
                d.UnitPrice,
                d.VendorId,
                d.Vendor?.StoreName,
                d.ShippingStatus,
                d.Carrier,
                d.TrackingNumber
            )),
            order.User?.FullName,
            order.User?.Email
        );
    }

    private static IEnumerable<OrderDto> MapToDtos(IEnumerable<Order> orders)
        => orders.Select(MapToDto);
}
