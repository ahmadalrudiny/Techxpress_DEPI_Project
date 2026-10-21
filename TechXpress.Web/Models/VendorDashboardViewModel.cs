using TechXpress.Services.Models;

namespace TechXpress.Web.Models;

/// <summary>Data shown on the vendor's own dashboard (لوحة تحكم البائع).</summary>
public class VendorDashboardViewModel
{
    public string StoreName { get; set; } = null!;

    /// <summary>VendorStatus name: Pending, Approved or Rejected.</summary>
    public string Status { get; set; } = null!;

    public bool IsApproved { get; set; }

    public string? RejectionReason { get; set; }

    public decimal Balance { get; set; }

    public VendorDashboardStats Stats { get; set; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0m, 0);

    public IReadOnlyList<VendorOrderSummaryDto> RecentOrders { get; set; } = new List<VendorOrderSummaryDto>();

    public IReadOnlyList<ProductDto> RecentProducts { get; set; } = new List<ProductDto>();

    public IReadOnlyList<WalletTransactionItem> RecentTransactions { get; set; } = new List<WalletTransactionItem>();

    /// <summary>The current store link, used to jump to the storefront filtered by this vendor.</summary>
    public int VendorId { get; set; }
}
