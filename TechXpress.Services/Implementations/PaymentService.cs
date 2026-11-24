using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TechXpress.Data.Entities;
using TechXpress.Data.UoW;
using TechXpress.Services.Interfaces;

namespace TechXpress.Services.Implementations;

public class PaymentService : IPaymentService
{
    /// <summary>اسم HttpClient المسجَّل في DI (يُضبط عنوانه وترويساته في Program.cs).</summary>
    public const string HttpClientName = "Stripe";

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentService> _logger;
    private readonly IWalletService _walletService;
    private readonly string _baseUrl;
    private readonly string _secretKey;
    private readonly HttpClient _httpClient;

    public PaymentService(IUnitOfWork unitOfWork, ILogger<PaymentService> logger,
        IWalletService walletService, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _walletService = walletService;
        _baseUrl = (configuration["AppSettings:BaseUrl"] ?? "https://localhost:7001").TrimEnd('/');
        _secretKey = configuration["Stripe:SecretKey"] ?? string.Empty;

        if (!IsUsableKey(_secretKey))
        {
            _logger.LogWarning(
                "Stripe:SecretKey is missing or still set to the appsettings placeholder, " +
                "so online payment will be unavailable. Configure it in user secrets or app settings.");
        }

        _httpClient = httpClientFactory.CreateClient(HttpClientName);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_secretKey}:")));
    }

    /// <summary>
    /// A key is only usable when it looks like a real Stripe key. The <c>sk_test_...</c>
    /// placeholder shipped in appsettings.json is deliberately rejected.
    /// </summary>
    private static bool IsUsableKey(string? key)
        => !string.IsNullOrWhiteSpace(key) && key.Length > 20 && key.StartsWith("sk_", StringComparison.Ordinal);

    public async Task<(bool Success, string Message, string? SessionUrl, string? SessionId)> CreateCheckoutSessionAsync(
        int orderId, string? baseUrl = null)
    {
        if (!IsUsableKey(_secretKey))
        {
            return (false, "خدمة الدفع الإلكتروني غير مُهيّأة حاليًا. يُرجى التواصل مع الدعم.", null, null);
        }

        var order = await _unitOfWork.Orders.GetByIdWithDetailsAsync(orderId);
        if (order == null)
        {
            return (false, "الطلب غير موجود.", null, null);
        }

        var returnBaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? _baseUrl : baseUrl.TrimEnd('/');

        var details = order.OrderDetails.ToList();
        if (details.Count == 0)
        {
            return (false, "لا يمكن الدفع لأن الطلب لا يحتوي على منتجات.", null, null);
        }

        // إذا كانت هناك جلسة سابقة ما زالت مفتوحة نستأنفها، وإن كانت مدفوعة نؤكّدها فوراً
        // (يحدث هذا عندما يدفع العميل ثم يغلق المتصفح قبل العودة لصفحة النجاح).
        var resumed = await TryResumeSessionAsync(order, returnBaseUrl);
        if (resumed is not null)
        {
            return resumed.Value;
        }

        // Stripe only accepts application/x-www-form-urlencoded bodies and rejects negative
        // line item amounts, so any promo discount is spread proportionally over the items
        // and the resulting total matches Order.TotalAmount to the cent.
        var form = new List<KeyValuePair<string, string>>
        {
            new("mode", "payment"),
            new("success_url", $"{returnBaseUrl}/Checkout/Success?session_id={{CHECKOUT_SESSION_ID}}&orderId={order.Id}"),
            new("cancel_url", $"{returnBaseUrl}/Checkout/Cancel?orderId={order.Id}"),
            new("payment_method_types[0]", "card"),
            new("metadata[OrderId]", order.Id.ToString())
        };

        var rawCents = details
            .Select(d => (long)Math.Round(d.UnitPrice * 100m) * d.Quantity)
            .ToList();
        var rawTotal = rawCents.Sum();
        var targetCents = (long)Math.Round(order.TotalAmount * 100m);

        if (rawTotal > 0 && targetCents < rawTotal)
        {
            // Split the discounted total over the items, keeping the sum exact.
            for (var i = 0; i < rawCents.Count; i++)
                rawCents[i] = rawCents[i] * targetCents / rawTotal;

            rawCents[rawCents.Count - 1] += targetCents - rawCents.Sum();
        }

        for (var i = 0; i < details.Count; i++)
        {
            var detail = details[i];
            var name = detail.Product?.Name ?? $"Product #{detail.ProductId}";
            var quantity = detail.Quantity;
            var lineTotal = rawCents[i];

            // Prefer keeping the real quantity; fall back to a single priced line when the
            // discounted amount cannot be divided evenly by the quantity.
            if (quantity <= 0 || lineTotal % quantity != 0)
            {
                quantity = 1;
                name = $"{name} (x{detail.Quantity})";
            }

            form.Add(new KeyValuePair<string, string>($"line_items[{i}][price_data][currency]", "usd"));
            form.Add(new KeyValuePair<string, string>($"line_items[{i}][price_data][unit_amount]", (lineTotal / quantity).ToString()));
            form.Add(new KeyValuePair<string, string>($"line_items[{i}][price_data][product_data][name]", name));
            form.Add(new KeyValuePair<string, string>($"line_items[{i}][quantity]", quantity.ToString()));
        }

        var content = new FormUrlEncodedContent(form);

        try
        {
            var response = await _httpClient.PostAsync("https://api.stripe.com/v1/checkout/sessions", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Stripe API error {StatusCode}: {Body}", (int)response.StatusCode, responseBody);
                return (false, "تعذّر بدء عملية الدفع. يُرجى المحاولة مرة أخرى.", null, null);
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            var sessionUrl = root.GetProperty("url").GetString();
            var sessionId = root.GetProperty("id").GetString();

            // نحتفظ بمعرّف الجلسة حتى نتمكن من استئنافها أو تأكيدها عند إعادة محاولة الدفع
            if (!string.IsNullOrWhiteSpace(sessionId) && order.StripeSessionId != sessionId)
            {
                order.StripeSessionId = sessionId;
                _unitOfWork.Orders.Update(order);
                await _unitOfWork.SaveChangesAsync();
            }

            _logger.LogInformation("Stripe Checkout Session {SessionId} created for order {OrderId}",
                sessionId, order.Id);

            return (true, "تم إنشاء جلسة الدفع.", sessionUrl, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Stripe Checkout Session for order {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// يفحص جلسة Stripe السابقة للطلب:
    /// - إن كانت مفتوحة: يعيد رابطها لاستئناف الدفع بدل إنشاء جلسة جديدة (يمنع الدفع المزدوج).
    /// - إن كانت مكتملة ومدفوعة: يؤكّد الطلب فوراً ويعيد رابط صفحة النجاح
    ///   (يحدث عندما يدفع العميل ثم يغلق المتصفح قبل الرجوع لصفحة النجاح).
    /// - إن كانت منتهية أو غير موجودة: يعيد null لإنشاء جلسة جديدة.
    /// </summary>
    private async Task<(bool Success, string Message, string? SessionUrl, string? SessionId)?> TryResumeSessionAsync(
        Order order, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(order.StripeSessionId))
        {
            return null;
        }

        try
        {
            var response = await _httpClient.GetAsync(
                $"https://api.stripe.com/v1/checkout/sessions/{order.StripeSessionId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Existing Stripe session {SessionId} for order {OrderId} could not be loaded ({StatusCode}); creating a new one.",
                    order.StripeSessionId, order.Id, (int)response.StatusCode);
                return null;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            var status = root.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : null;
            var paymentStatus = root.TryGetProperty("payment_status", out var paymentElement)
                ? paymentElement.GetString()
                : null;

            if (string.Equals(status, "open", StringComparison.OrdinalIgnoreCase))
            {
                var url = root.TryGetProperty("url", out var urlElement) ? urlElement.GetString() : null;
                if (!string.IsNullOrWhiteSpace(url))
                {
                    _logger.LogInformation("Resuming open Stripe session {SessionId} for order {OrderId}",
                        order.StripeSessionId, order.Id);
                    return (true, "تم استئناف جلسة الدفع السابقة.", url, order.StripeSessionId);
                }
            }

            if (string.Equals(status, "complete", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(paymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
            {
                var (confirmed, message) = await ConfirmPaymentAsync(order.StripeSessionId!);
                if (confirmed)
                {
                    var successUrl =
                        $"{baseUrl}/Checkout/Success?session_id={order.StripeSessionId}&orderId={order.Id}";
                    return (true, message, successUrl, order.StripeSessionId);
                }
            }
        }
        catch (Exception ex)
        {
            // فشل الفحص لا يمنع إنشاء جلسة جديدة
            _logger.LogWarning(ex,
                "Could not inspect existing Stripe session {SessionId} for order {OrderId}",
                order.StripeSessionId, order.Id);
        }

        return null;
    }

    public async Task<(bool Success, string Message)> ConfirmPaymentAsync(string sessionId)
    {
        if (!IsUsableKey(_secretKey))
        {
            return (false, "خدمة الدفع الإلكتروني غير مُهيّأة حاليًا. يُرجى التواصل مع الدعم.");
        }

        try
        {
            var response = await _httpClient.GetAsync($"https://api.stripe.com/v1/checkout/sessions/{sessionId}");
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Stripe API error {StatusCode}: {Body}", (int)response.StatusCode, responseBody);
                return (false, "جلسة الدفع غير موجودة.");
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            var orderIdStr = root.GetProperty("metadata").GetProperty("OrderId").GetString();
            if (orderIdStr == null || !int.TryParse(orderIdStr, out var orderId))
            {
                return (false, "بيانات جلسة الدفع غير صالحة.");
            }

            var order = await _unitOfWork.Orders.GetByIdWithDetailsAsync(orderId);
            if (order == null)
            {
                return (false, "الطلب غير موجود.");
            }

            // Idempotent: لا نُعيد الطلب لحالة "مدفوع" لو تم شحنه أو توصيله بالفعل
            if (order.Status is OrderStatus.Paid or OrderStatus.Shipped or OrderStatus.Delivered)
            {
                _logger.LogInformation("Order {OrderId} already paid (idempotent confirmation)", orderId);
                return (true, "تم تأكيد الدفع مسبقاً.");
            }

            if (order.Status is OrderStatus.Cancelled or OrderStatus.Returned)
            {
                return (false, "لا يمكن تأكيد الدفع لطلب ملغي أو مُرجَع.");
            }

            var paymentStatus = root.GetProperty("payment_status").GetString();
            if (paymentStatus != "paid")
            {
                _logger.LogWarning("Session {SessionId} payment status is {Status}, not paid", sessionId, paymentStatus);
                return (false, "لم تكتمل عملية الدفع.");
            }

            // Verify amount matches
            var sessionTotal = root.GetProperty("amount_total").GetDecimal() / 100m;
            if (Math.Abs(sessionTotal - order.TotalAmount) > 0.01m)
            {
                _logger.LogWarning("Amount mismatch: session {SessionTotal} vs order {OrderTotal}", sessionTotal, order.TotalAmount);
                return (false, "قيمة الدفع لا تطابق إجمالي الطلب.");
            }

            var paymentIntentId = root.GetProperty("payment_intent").GetString();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                order.Status = OrderStatus.Paid;
                order.PaymentIntentId = paymentIntentId;
                _unitOfWork.Orders.Update(order);

                await _unitOfWork.CommitAsync();

                // ONLY AFTER commit: process commission
                await _walletService.ProcessOrderCommissionAsync(orderId);

                _logger.LogInformation("Order {OrderId} marked as Paid via Stripe session {SessionId}", orderId, sessionId);
                return (true, "تم تأكيد الدفع بنجاح.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Failed to confirm payment for order {OrderId}", orderId);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm payment for session {SessionId}", sessionId);
            throw;
        }
    }
}
