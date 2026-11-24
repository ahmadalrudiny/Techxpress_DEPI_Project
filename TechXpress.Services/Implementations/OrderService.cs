using System.Text.Json;
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
        string userId, string cartJson, string shippingAddress, string? promoCode = null,
        bool useStoreCredit = false)
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

            // 3. Apply store credit (نقاط العميل) when requested
            decimal storeCreditUsed = 0m;
            if (useStoreCredit)
            {
                var balance = await _unitOfWork.StoreCredits.GetBalanceAsync(userId);
                if (balance > 0)
                    storeCreditUsed = Math.Min(balance, totalAmount);
            }

            var payableAmount = Math.Round(totalAmount - storeCreditUsed, 2, MidpointRounding.AwayFromZero);
            if (payableAmount < 0) payableAmount = 0;

            // 4. Create Order
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = payableAmount,
                StoreCreditUsed = storeCreditUsed,
                DiscountAmount = discountAmount,
                PromoCode = appliedPromoCode,
                Status = payableAmount <= 0 ? OrderStatus.Paid : OrderStatus.Pending,
                PaymentIntentId = payableAmount <= 0 ? "STORE_CREDIT" : null,
                ShippingAddress = shippingAddress
            };

            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveChangesAsync(); // to get order.Id

            // 5. Deduct the used credit and keep an audit trail
            if (storeCreditUsed > 0)
            {
                var user = await _unitOfWork.StoreCredits.GetUserAsync(userId);
                if (user == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return (false, "تعذر العثور على حساب العميل.", null);
                }

                user.StoreCreditBalance -= storeCreditUsed;
                await _unitOfWork.StoreCredits.AddTransactionAsync(new StoreCreditTransaction
                {
                    UserId = userId,
                    Amount = -storeCreditUsed,
                    BalanceAfter = user.StoreCreditBalance,
                    Type = StoreCreditTransactionType.OrderPayment,
                    OrderId = order.Id,
                    Description = Clamp("استخدام رصيد النقاط في الطلب", 300)
                });
            }

            // 6. Create OrderDetails with UnitPrice from database, VendorId from Product.VendorId
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

            // 7. Orders completely covered by credit are already paid -> record vendor/platform earnings
            if (payableAmount <= 0)
            {
                try
                {
                    await _walletService.ProcessOrderCommissionAsync(order.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process commission for credit-paid order {OrderId}", order.Id);
                }
            }

            _logger.LogInformation("Order {OrderId} placed for user {UserId} (credit used: {Credit})",
                order.Id, userId, storeCreditUsed);

            var message = storeCreditUsed > 0
                ? $"تم إنشاء طلبك بنجاح وتم استخدام ${storeCreditUsed:F2} من رصيد نقاطك."
                : "تم إنشاء طلبك بنجاح.";

            return (true, message, order.Id);
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
        // Cancellation and return both need refund/stock/commission side effects.
        if (newStatus == OrderStatus.Cancelled)
            return await CancelOrderAsync(id, null);

        if (newStatus == OrderStatus.Returned)
            return await ReturnOrderAsync(id, null);

        var order = await _unitOfWork.Orders.GetByIdWithDetailsAsync(id);
        if (order == null)
            return (false, "الطلب غير موجود.");

        // Allowed transitions
        bool allowed = (order.Status, newStatus) switch
        {
            (OrderStatus.Pending, OrderStatus.Paid) => true,
            (OrderStatus.Paid, OrderStatus.Shipped) => true,
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

            // When an admin marks a pending order as paid, vendors/platform must get their share.
            if (newStatus == OrderStatus.Paid)
            {
                try
                {
                    await _walletService.ProcessOrderCommissionAsync(id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process commission for order {OrderId} marked paid by admin", id);
                }
            }

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

        if (order.Status == OrderStatus.Returned)
            return (false, "تم إرجاع هذا الطلب بالفعل.");

        if (order.Status == OrderStatus.Delivered)
            return (false, "لا يمكن إلغاء طلب تم توصيله. استخدم خيار إرجاع الطلب.");

        var wasPaid = order.Status is OrderStatus.Paid or OrderStatus.Shipped;
        var alreadyRefunded = await _unitOfWork.StoreCredits.HasRefundForOrderAsync(id);

        // Paid orders refund the full value (cash + used credit) as store credit.
        // Pending orders only need the reserved credit to be returned.
        var refundAmount = wasPaid
            ? order.TotalAmount + order.StoreCreditUsed
            : order.StoreCreditUsed;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // Restore stock
            foreach (var detail in order.OrderDetails)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(detail.ProductId);
                if (product != null)
                {
                    product.StockQuantity += detail.Quantity;
                    _unitOfWork.Products.Update(product);
                }
            }

            // Refund the value as points on the customer's account
            if (!alreadyRefunded && refundAmount > 0)
            {
                var user = await _unitOfWork.StoreCredits.GetUserAsync(order.UserId);
                if (user != null)
                {
                    user.StoreCreditBalance += refundAmount;

                    await _unitOfWork.StoreCredits.AddTransactionAsync(new StoreCreditTransaction
                    {
                        UserId = order.UserId,
                        Amount = refundAmount,
                        BalanceAfter = user.StoreCreditBalance,
                        Type = StoreCreditTransactionType.OrderRefund,
                        OrderId = order.Id,
                        Description = Clamp(
                            wasPaid
                                ? "استرداد قيمة الطلب كنقاط بعد الإلغاء"
                                : "إرجاع رصيد النقاط المستخدم في الطلب",
                            300)
                    });
                }
            }

            order.Status = OrderStatus.Cancelled;
            _unitOfWork.Orders.Update(order);

            await _unitOfWork.CommitAsync();

            // Reverse vendor + platform earnings outside the order transaction
            if (wasPaid && !alreadyRefunded)
            {
                await _walletService.ReverseOrderCommissionAsync(id);
                _logger.LogInformation("Commission reversed for cancelled order {OrderId}", id);
            }

            _logger.LogInformation("Order {OrderId} cancelled by user {UserId}", id, userId);

            if (wasPaid && refundAmount > 0)
                return (true, $"تم إلغاء الطلب واستعادة الكميات، وأُضيف ${refundAmount:F2} إلى رصيد نقاطك.");

            if (!wasPaid && refundAmount > 0)
                return (true, "تم إلغاء الطلب واستعادة الكميات ورصيد النقاط المستخدم.");

            return (true, "تم إلغاء الطلب واستعادة الكميات إلى المخزون.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Failed to cancel order {OrderId}", id);
            throw;
        }
    }

    public async Task<(bool Success, string Message)> ReturnOrderAsync(int id, string? userId)
    {
        var order = userId == null
            ? await _unitOfWork.Orders.GetByIdWithDetailsAsync(id)
            : await _unitOfWork.Orders.GetByIdForUserAsync(id, userId);
        if (order == null)
            return (false, "الطلب غير موجود أو لا تملك صلاحية الوصول إليه.");

        if (order.Status != OrderStatus.Delivered)
            return (false, "يمكن إرجاع الطلبات التي تم توصيلها فقط.");

        if (await _unitOfWork.StoreCredits.HasRefundForOrderAsync(id))
            return (false, "تم إرجاع هذا الطلب واسترداد قيمته بالفعل.");

        var refundAmount = order.TotalAmount + order.StoreCreditUsed;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // Restore stock
            foreach (var detail in order.OrderDetails)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(detail.ProductId);
                if (product != null)
                {
                    product.StockQuantity += detail.Quantity;
                    _unitOfWork.Products.Update(product);
                }
            }

            // Refund the full value as store credit
            if (refundAmount > 0)
            {
                var user = await _unitOfWork.StoreCredits.GetUserAsync(order.UserId);
                if (user == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return (false, "تعذر العثور على حساب العميل.");
                }

                user.StoreCreditBalance += refundAmount;

                await _unitOfWork.StoreCredits.AddTransactionAsync(new StoreCreditTransaction
                {
                    UserId = order.UserId,
                    Amount = refundAmount,
                    BalanceAfter = user.StoreCreditBalance,
                    Type = StoreCreditTransactionType.OrderRefund,
                    OrderId = order.Id,
                    Description = Clamp("استرداد قيمة الطلب كنقاط بعد الإرجاع", 300)
                });
            }

            order.Status = OrderStatus.Returned;
            _unitOfWork.Orders.Update(order);

            await _unitOfWork.CommitAsync();

            await _walletService.ReverseOrderCommissionAsync(id);

            _logger.LogInformation("Order {OrderId} returned by user {UserId}", id, userId);
            return (true, $"تم إرجاع الطلب وأُضيف ${refundAmount:F2} إلى رصيد نقاطك.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Failed to return order {OrderId}", id);
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
            order.StoreCreditUsed,
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

    private static string Clamp(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
