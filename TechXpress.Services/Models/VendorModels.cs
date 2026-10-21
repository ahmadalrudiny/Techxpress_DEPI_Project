using TechXpress.Data.Entities;

namespace TechXpress.Services.Models;

public enum VendorDocumentType { NationalId, CommercialRegister, TaxCard }

public record UploadedFile(Stream Content, string FileName, long Length);

public record VendorApplicationRequest(
    string StoreName,
    UploadedFile NationalId,
    UploadedFile CommercialRegister,
    UploadedFile TaxCard);

public record VendorApplicationStatus(
    int VendorId, string StoreName, VendorStatus Status,
    string? RejectionReason, DateTime SubmittedAt);

public record VendorRequestItem(
    int Id, string StoreName, string OwnerName, string OwnerEmail,
    DateTime SubmittedAt, VendorStatus Status, string? RejectionReason);

public record StoredDocument(Stream Content, string ContentType);

public record DocumentSaveResult(bool Success, string? StoredName, string? Error);

// ===== طلبات المتجر الخاصة بالبائع (Seller Order Fulfillment) =====

public record VendorOrderSummaryDto(
    int OrderId,
    DateTime OrderDate,
    OrderStatus Status,
    int ItemsCount,
    decimal VendorTotal,
    ShippingStatus ShippingStatus);

public record VendorOrderItemDto(
    int ProductId,
    string ProductName,
    string? ImageUrl,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    ShippingStatus ShippingStatus);

public record VendorOrderDetailsDto(
    int OrderId,
    DateTime OrderDate,
    OrderStatus Status,
    string? ShippingAddress,
    IReadOnlyList<VendorOrderItemDto> Items,
    ShippingStatus ShippingStatus,
    string? Carrier,
    string? TrackingNumber,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    string? CustomerName = null,
    string? CustomerPhone = null)
{
    public decimal VendorTotal => Items.Sum(i => i.LineTotal);
}

// ===== سجل طلبات سحب الأرباح (لوحة الأدمن) =====

/// <summary>One payout (withdrawal) request with its review audit trail.</summary>
public record PayoutRequestItem(
    int Id,
    int VendorId,
    string StoreName,
    decimal Amount,
    string? PayoutDetails,
    string? Note,
    WalletTransactionStatus Status,
    DateTime RequestedAt,
    DateTime? ProcessedAtUtc,
    string? ProcessedByName,
    string? RejectionReason);

/// <summary>Payout request counters used by the admin history tabs.</summary>
public record PayoutCounts(int Total, int Pending, int Approved, int Rejected);

// ===== إحصائيات لوحة تحكم البائع =====

/// <summary>Aggregated numbers shown on the vendor dashboard.</summary>
public record VendorDashboardStats(
    int TotalProducts,
    int ActiveProducts,
    int InactiveProducts,
    int TotalOrders,
    int AwaitingShipment,
    int ShippedOrders,
    int DeliveredOrders,
    int CancelledOrders,
    decimal TotalSales,
    int PendingPayouts);