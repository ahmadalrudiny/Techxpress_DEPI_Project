namespace TechXpress.Services.Interfaces;

public interface IPaymentService
{
    /// <summary>
    /// Creates a Stripe Checkout Session for the order. Line items are built from the database order.
    /// </summary>
    /// <param name="orderId">Identifier of the order that is being paid for.</param>
    /// <param name="baseUrl">
    /// Optional public base URL used to build the Stripe success/cancel return URLs, for example
    /// <c>https://shop.example.com</c>. When omitted, the value configured under
    /// <c>AppSettings:BaseUrl</c> is used.
    /// </param>
    /// <returns>
    /// A tuple describing the outcome: success flag, human readable message, the hosted checkout
    /// URL the customer must be redirected to, and the Stripe session identifier.
    /// </returns>
    Task<(bool Success, string Message, string? SessionUrl, string? SessionId)> CreateCheckoutSessionAsync(int orderId, string? baseUrl = null);

    /// <summary>
    /// Confirms payment by retrieving the Stripe session, verifying payment_status == paid,
    /// saving PaymentIntentId, setting order to Paid, and calling ProcessOrderCommissionAsync after commit.
    /// Idempotent if the success page is refreshed.
    /// </summary>
    Task<(bool Success, string Message)> ConfirmPaymentAsync(string sessionId);
}
