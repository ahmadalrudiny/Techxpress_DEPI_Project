using TechXpress.Data.Entities;
using TechXpress.Services.Models;

namespace TechXpress.Services.Interfaces;

public interface IWalletService
{
    /// <summary>Deposits vendors' net earnings after an order is paid. Safe to call more than once.</summary>
    Task ProcessOrderCommissionAsync(int orderId);

    /// <summary>Reverses vendor earnings when a paid order is cancelled. Idempotent. Allows negative balance.</summary>
    Task ReverseOrderCommissionAsync(int orderId);

    /// <summary>Creates a payout request. The amount is reserved (deducted) immediately.</summary>
    Task<(bool Success, string Message)> RequestPayoutAsync(
        int vendorId, decimal amount, string payoutDetails, string? note = null);

    /// <summary>
    /// Approves or rejects a payout request. A rejection returns the reserved amount.
    /// Records the decision time, the deciding administrator and the rejection reason.
    /// </summary>
    /// <param name="transactionId">Withdrawal transaction identifier.</param>
    /// <param name="approve">True to approve (pay out), false to reject and refund the reserved amount.</param>
    /// <param name="adminUserId">Identifier of the administrator taking the decision.</param>
    /// <param name="rejectionReason">Reason shown to the vendor when rejecting.</param>
    /// <param name="adminName">Display name/e-mail of the administrator (stored for the audit trail).</param>
    Task<(bool Success, string Message)> ProcessPayoutAsync(
        int transactionId, bool approve, string adminUserId,
        string? rejectionReason = null, string? adminName = null);

    /// <summary>Gets the wallet history of one vendor, newest first.</summary>
    Task<IEnumerable<WalletTransaction>> GetVendorTransactionsAsync(int vendorId);

    /// <summary>Gets all pending payout requests (admin).</summary>
    Task<IEnumerable<WalletTransaction>> GetPendingPayoutRequestsAsync();

    /// <summary>
    /// Gets payout requests for the admin history page, newest first, optionally filtered by status.
    /// </summary>
    /// <param name="status">Optional status filter (<c>null</c> returns every request).</param>
    Task<IReadOnlyList<PayoutRequestItem>> GetPayoutRequestsAsync(WalletTransactionStatus? status);

    /// <summary>Counts payout requests per status for the admin history tabs.</summary>
    Task<PayoutCounts> GetPayoutRequestCountsAsync();

    /// <summary>Gets the wallet summary of the logged-in vendor. Returns null if not an approved vendor.</summary>
    Task<VendorWalletSummary?> GetWalletSummaryAsync(string userId);

    /// <summary>Creates a payout request for the vendor that owns this user account.</summary>
    Task<(bool Success, string Message)> RequestPayoutForUserAsync(
        string userId, decimal amount, string payoutDetails, string? note = null);
}