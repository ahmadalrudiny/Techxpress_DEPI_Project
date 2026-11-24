using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechXpress.Data.Entities;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;
using TechXpress.Web.Infrastructure;
using TechXpress.Web.Models;

namespace TechXpress.Web.Controllers;

[Authorize(Roles = "Customer")]
public class CheckoutController : Controller
{
    private readonly ICartService _cartService;
    private readonly ICartPersistenceService _cartPersistence;
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;
    private readonly IStoreCreditService _storeCreditService;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(
        ICartService cartService,
        ICartPersistenceService cartPersistence,
        IOrderService orderService,
        IPaymentService paymentService,
        IStoreCreditService storeCreditService,
        ILogger<CheckoutController> logger)
    {
        _cartService = cartService;
        _cartPersistence = cartPersistence;
        _orderService = orderService;
        _paymentService = paymentService;
        _storeCreditService = storeCreditService;
        _logger = logger;
    }

    private async Task<string> GetCartJsonAsync()
    {
        var cartJson = HttpContext.Session.GetString(CartSession.Key);
        if (!string.IsNullOrWhiteSpace(cartJson) && cartJson != CartSession.EmptyJson)
        {
            return cartJson;
        }

        var saved = await _cartPersistence.GetSavedCartAsync(CurrentUserId);
        if (!string.IsNullOrWhiteSpace(saved))
        {
            HttpContext.Session.SetString(CartSession.Key, saved);
            return saved;
        }

        return cartJson ?? CartSession.EmptyJson;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var cartJson = await GetCartJsonAsync();
        var summary = _cartService.GetSummary(cartJson);

        if (!summary.Items.Any())
        {
            TempData["ErrorMessage"] = "سلة الشراء فارغة.";
            return RedirectToAction("Index", "Cart");
        }

        var model = new CheckoutViewModel
        {
            ShippingAddress = string.Empty,
            Subtotal = summary.Subtotal,
            Discount = summary.Discount,
            Total = summary.Total,
            ItemCount = summary.ItemCount,
            AvailableCredit = await _storeCreditService.GetBalanceAsync(CurrentUserId)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var validationCartJson = await GetCartJsonAsync();
            var validationSummary = _cartService.GetSummary(validationCartJson);
            model.Subtotal = validationSummary.Subtotal;
            model.Discount = validationSummary.Discount;
            model.Total = validationSummary.Total;
            model.ItemCount = validationSummary.ItemCount;
            model.AvailableCredit = await _storeCreditService.GetBalanceAsync(CurrentUserId);
            return View("Checkout", model);
        }

        var cartJson = await GetCartJsonAsync();
        var summary = _cartService.GetSummary(cartJson, model.PromoCode);

        if (!summary.Items.Any())
        {
            TempData["ErrorMessage"] = "سلة الشراء فارغة.";
            return RedirectToAction("Index", "Cart");
        }

        // لو العميل رجع من صفحة دفع فاشلة أو ملغاة وضغط «إتمام الشراء» تاني بنفس السلة،
        // بنستأنف نفس الطلب المعلّق بدل ما ننشئ طلب مكرر ونحجز مخزون إضافي.
        var expectedCredit = 0m;
        if (model.UseStoreCredit)
        {
            var balance = await _storeCreditService.GetBalanceAsync(CurrentUserId);
            expectedCredit = Math.Min(balance, summary.Total);
        }

        var userOrders = await _orderService.GetUserOrdersAsync(CurrentUserId);
        var matchingPendingOrder = userOrders
            .Where(o => o.Status == OrderStatus.Pending)
            .OrderByDescending(o => o.OrderDate)
            .FirstOrDefault(o => MatchesCart(o, summary, expectedCredit));

        if (matchingPendingOrder != null)
        {
            return RedirectToAction(nameof(Payment), new { orderId = matchingPendingOrder.Id });
        }

        var (success, message, orderId) = await PlaceOrderSafelyAsync(cartJson, model);

        if (!success || orderId == null)
        {
            TempData["ErrorMessage"] = message;
            return RedirectToAction("Checkout");
        }

        var createdOrder = await _orderService.GetOrderAsync(orderId.Value, CurrentUserId);

        // الطلب المدفوع بالكامل من رصيد النقاط لا يحتاج جلسة Stripe.
        if (createdOrder != null && createdOrder.TotalAmount <= 0)
        {
            return RedirectToAction(nameof(Success), new { orderId = orderId.Value });
        }

        // مهم: لا نُفرّغ السلة هنا. السلة تفضل كما هي لو فشل الدفع أو أُلغى،
        // وتُفرّغ فقط بعد تأكيد نجاح الدفع في Success.
        return RedirectToAction(nameof(Payment), new { orderId = orderId.Value });
    }

    /// <summary>ينشئ الطلب مع معالجة الأخطاء غير المتوقعة دون تعريض العميل لصفحة خطأ تقنية.</summary>
    private async Task<(bool Success, string Message, int? OrderId)> PlaceOrderSafelyAsync(
        string cartJson, CheckoutViewModel model)
    {
        try
        {
            return await _orderService.PlaceOrderAsync(
                CurrentUserId, cartJson, model.ShippingAddress, model.PromoCode, model.UseStoreCredit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to place order for user {UserId}", CurrentUserId);
            return (false,
                "تعذّر إنشاء طلبك في الوقت الحالي. لم يتم خصم أي مبلغ، وسلتك محفوظة كما هي. يُرجى المحاولة مرة أخرى.",
                null);
        }
    }

    /// <summary>يقارن عناصر السلة الحالية بطلب معلّق سابق لنفس العميل (نفس المنتجات والكميات والإجمالي).</summary>
    private static bool MatchesCart(OrderDto order, CartSummaryDto summary, decimal expectedCredit)
    {
        var cartItems = summary.Items.ToList();
        var orderItems = order.OrderDetails.ToList();
        if (orderItems.Count != cartItems.Count)
        {
            return false;
        }

        var orderQuantities = orderItems
            .GroupBy(d => d.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.Quantity));

        foreach (var item in cartItems)
        {
            if (!orderQuantities.TryGetValue(item.ProductId, out var quantity) || quantity != item.Quantity)
            {
                return false;
            }
        }

        // TotalAmount is the cash paid after credit, so compare the full order value.
        // When the customer does not select credit (expectedCredit == 0) a pending order
        // that already holds reserved credit still matches, so retrying payment reuses it
        // instead of creating a duplicate order (the reserved credit is not in the balance).
        var preCreditTotalMatches =
            Math.Abs(order.TotalAmount + order.StoreCreditUsed - summary.Total) < 0.01m;

        var creditMatches = expectedCredit <= 0
            || Math.Abs(order.StoreCreditUsed - expectedCredit) < 0.01m;

        return preCreditTotalMatches && creditMatches;
    }

    [HttpGet]
    public async Task<IActionResult> Payment(int orderId)
    {
        // Verify ownership
        var order = await _orderService.GetOrderAsync(orderId, CurrentUserId);
        if (order == null)
        {
            TempData["ErrorMessage"] = "الطلب غير موجود أو لا تملك صلاحية الوصول إليه.";
            return RedirectToAction("Index", "Home");
        }

        // لا يُسمح بدفع طلب ملغي أو مدفوع بالفعل
        if (order.Status != OrderStatus.Pending)
        {
            TempData["ErrorMessage"] = order.Status switch
            {
                OrderStatus.Cancelled => "لا يمكن دفع طلب ملغي.",
                OrderStatus.Returned => "لا يمكن دفع طلب تم إرجاعه.",
                _ => "تم دفع هذا الطلب بالفعل."
            };
            return RedirectToAction("OrderDetails", "Profile", new { id = orderId });
        }

        // نستخدم العنوان الحالي للموقع حتى تعود روابط النجاح/الإلغاء إلى نفس المضيف الذي يستخدمه العميل
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        try
        {
            var (success, message, sessionUrl, _) = await _paymentService.CreateCheckoutSessionAsync(orderId, baseUrl);

            if (!success || string.IsNullOrEmpty(sessionUrl))
            {
                TempData["PaymentError"] = message ?? "تعذّر بدء عملية الدفع. يُرجى المحاولة مرة أخرى.";
                return RedirectToAction(nameof(PaymentError), new { orderId });
            }

            return Redirect(sessionUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Stripe checkout for order {OrderId}", orderId);
            TempData["PaymentError"] =
                "تعذّر الاتصال ببوابة الدفع في الوقت الحالي. تحقّق من اتصالك بالإنترنت ثم حاول مرة أخرى.";
            return RedirectToAction(nameof(PaymentError), new { orderId });
        }
    }

    /// <summary>
    /// صفحة معالجة أخطاء الدفع: تُعرض عند تعذّر إنشاء جلسة Stripe أو عند فشل تأكيد الدفع.
    /// السلة تظل محفوظة كما هي حتى ينجح الدفع.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> PaymentError(int? orderId)
    {
        if (orderId.HasValue)
        {
            var order = await _orderService.GetOrderAsync(orderId.Value, CurrentUserId);
            ViewData["OrderId"] = order?.Id;
            ViewData["OrderTotal"] = order?.TotalAmount;
        }

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Success(
        [FromQuery(Name = "session_id")] string? sessionId,
        [FromQuery(Name = "orderId")] int? orderId)
    {
        if (!orderId.HasValue)
        {
            TempData["ErrorMessage"] = "بيانات الدفع غير مكتملة.";
            return RedirectToAction("Index", "Home");
        }

        // نتحقق أن الطلب يخص المستخدم الحالي قبل تأكيد الدفع
        var order = await _orderService.GetOrderAsync(orderId.Value, CurrentUserId);
        if (order == null)
        {
            TempData["ErrorMessage"] = "الطلب غير موجود أو لا تملك صلاحية الوصول إليه.";
            return RedirectToAction("Index", "Home");
        }

        // طلب مدفوع بالكامل من رصيد النقاط: لا توجد جلسة Stripe لتأكيدها.
        if (string.IsNullOrEmpty(sessionId))
        {
            if (order.Status is OrderStatus.Paid or OrderStatus.Shipped or OrderStatus.Delivered)
            {
                HttpContext.Session.SetString(CartSession.Key, CartSession.EmptyJson);
                await _cartPersistence.ClearAsync(CurrentUserId);

                TempData["SuccessMessage"] = order.StoreCreditUsed > 0
                    ? "تم إتمام طلبك باستخدام رصيد النقاط بنجاح."
                    : "تم تأكيد طلبك بنجاح.";
                ViewData["OrderId"] = order.Id;
                return View();
            }

            TempData["ErrorMessage"] = "بيانات الدفع غير مكتملة.";
            return RedirectToAction("Index", "Home");
        }

        try
        {
            var (success, message) = await _paymentService.ConfirmPaymentAsync(sessionId);
            if (!success)
            {
                // الدفع لم يكتمل (أو تعذّر تأكيده) — لا نُفرّغ السلة ونعرض صفحة الخطأ
                TempData["PaymentError"] = message;
                return RedirectToAction(nameof(PaymentError), new { orderId = order.Id });
            }

            // تُفرّغ السلة فقط بعد نجاح الدفع وتأكيده
            HttpContext.Session.SetString(CartSession.Key, CartSession.EmptyJson);
            await _cartPersistence.ClearAsync(CurrentUserId);

            TempData["SuccessMessage"] = message;
            ViewData["OrderId"] = order.Id;
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm payment for order {OrderId}", order.Id);
            TempData["PaymentError"] =
                "استلمنا عملية الدفع لكن تعذّر تأكيدها الآن. اضغط «إعادة المحاولة» لإكمال التأكيد — لن يُخصم منك مبلغ إضافي.";
            return RedirectToAction(nameof(PaymentError), new { orderId = order.Id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Cancel(int? orderId)
    {
        // نتيح إعادة المحاولة مباشرةً من صفحة الإلغاء
        if (orderId.HasValue)
        {
            var order = await _orderService.GetOrderAsync(orderId.Value, CurrentUserId);
            if (order != null && order.Status == OrderStatus.Pending)
            {
                ViewData["RetryOrderId"] = order.Id;
                ViewData["RetryTotal"] = order.TotalAmount;
            }
        }

        // السلة تظل محفوظة بعد إلغاء الدفع، فنعرض عدد منتجاتها للتأكيد للعميل
        var cartJson = await GetCartJsonAsync();
        ViewData["CartItemCount"] = _cartService.GetSummary(cartJson).ItemCount;

        return View();
    }
}
