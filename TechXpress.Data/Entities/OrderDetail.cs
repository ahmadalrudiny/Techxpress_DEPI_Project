namespace TechXpress.Data.Entities;

public class OrderDetail
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public int? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    // ===== الشحن (مسؤولية البائع) =====

    /// <summary>حالة شحن هذا البند — يحدّثها البائع صاحب المنتج.</summary>
    public ShippingStatus ShippingStatus { get; set; } = ShippingStatus.Pending;

    /// <summary>وقت تسليم الشحنة لشركة الشحن.</summary>
    public DateTime? ShippedAt { get; set; }

    /// <summary>وقت تأكيد وصول الشحنة للعميل.</summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>شركة الشحن (مثال: Aramex، DHL).</summary>
    public string? Carrier { get; set; }

    /// <summary>رقم تتبع الشحنة (اختياري).</summary>
    public string? TrackingNumber { get; set; }
}
