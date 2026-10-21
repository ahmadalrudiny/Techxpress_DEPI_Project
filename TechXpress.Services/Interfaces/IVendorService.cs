using TechXpress.Data.Entities;
using TechXpress.Services.Models;

namespace TechXpress.Services.Interfaces;

public interface IVendorService
{
    /// <summary>Gets the application status of a user, or null if they never applied.</summary>
    Task<VendorApplicationStatus?> GetApplicationStatusAsync(string userId);

    /// <summary>Submits (or re-submits after rejection) a vendor application with its documents.</summary>
    Task<(bool Success, string Message)> ApplyAsync(string userId, VendorApplicationRequest request);

    /// <summary>Lists applications for the admin, optionally filtered by status.</summary>
    Task<IReadOnlyList<VendorRequestItem>> GetRequestsAsync(VendorStatus? status);

    /// <summary>Approves an application and grants the Vendor role.</summary>
    Task<(bool Success, string Message)> ApproveAsync(int vendorId, string adminUserId);

    /// <summary>Rejects an application with a reason.</summary>
    Task<(bool Success, string Message)> RejectAsync(int vendorId, string adminUserId, string reason);
    /// <summary>Opens one of the vendor's documents for the admin. Every access is logged.</summary>
    Task<StoredDocument?> OpenDocumentAsync(int vendorId, VendorDocumentType type, string adminUserId);

    /// <summary>Gets the vendor ID for a given user ID, or null if not a vendor.</summary>
    Task<int?> GetVendorIdByUserIdAsync(string userId);

    /// <summary>Returns true when the vendor exists and is approved.</summary>
    Task<bool> IsApprovedAsync(int vendorId);

    /// <summary>Gets order details for a vendor (orders containing his products).</summary>
    Task<IEnumerable<OrderDetailDto>> GetVendorOrderDetailsAsync(int vendorId);

    /// <summary>Gets the orders containing the vendor's products (grouped per order).</summary>
    Task<IReadOnlyList<VendorOrderSummaryDto>> GetVendorOrdersAsync(int vendorId);

    /// <summary>Gets a single order for a vendor (only the vendor's own line items).</summary>
    Task<VendorOrderDetailsDto?> GetVendorOrderAsync(int vendorId, int orderId);

    // ===== الشحن (مسؤولية البائع) =====

    /// <summary>Marks the vendor's items in an order as being prepared for shipment.</summary>
    Task<(bool Success, string Message)> StartPreparingAsync(int vendorId, int orderId);

    /// <summary>Ships the vendor's items with a carrier and an optional tracking number.</summary>
    Task<(bool Success, string Message)> ShipOrderAsync(int vendorId, int orderId, string carrier, string? trackingNumber);

    /// <summary>Confirms delivery of the vendor's shipped items.</summary>
    Task<(bool Success, string Message)> MarkDeliveredAsync(int vendorId, int orderId);

    /// <summary>
    /// Aggregates the numbers shown on the vendor dashboard
    /// (products, orders per status, total sales and pending payout requests).
    /// </summary>
    Task<VendorDashboardStats> GetDashboardStatsAsync(int vendorId);
}