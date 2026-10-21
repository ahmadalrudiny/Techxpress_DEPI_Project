using TechXpress.Data.Entities;
using TechXpress.Services.Models;

namespace TechXpress.Web.Areas.Admin.Models;

/// <summary>One row of the payout (withdrawal) request history.</summary>
public class PayoutRequestViewModel
{
    public int TransactionId { get; set; }
    public int VendorId { get; set; }
    public string StoreName { get; set; } = null!;
    public decimal Amount { get; set; }
    public string? PayoutDetails { get; set; }
    public string? Note { get; set; }
    public WalletTransactionStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessedByName { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>True when the request can still be approved or rejected.</summary>
    public bool IsPending => Status == WalletTransactionStatus.Pending;
}

/// <summary>Payout history page: the filtered rows plus the tab counters.</summary>
public class PayoutHistoryViewModel
{
    public IReadOnlyList<PayoutRequestViewModel> Requests { get; set; } = new List<PayoutRequestViewModel>();
    public PayoutCounts Counts { get; set; } = new(0, 0, 0, 0);

    /// <summary>Active status filter: <c>null</c> means "all".</summary>
    public WalletTransactionStatus? StatusFilter { get; set; }

    /// <summary>Total amount of the approved (paid out) requests.</summary>
    public decimal ApprovedTotal => Requests
        .Where(r => r.Status == WalletTransactionStatus.Completed)
        .Sum(r => r.Amount);

    /// <summary>Total amount currently reserved by pending requests.</summary>
    public decimal PendingTotal => Requests
        .Where(r => r.Status == WalletTransactionStatus.Pending)
        .Sum(r => r.Amount);
}
