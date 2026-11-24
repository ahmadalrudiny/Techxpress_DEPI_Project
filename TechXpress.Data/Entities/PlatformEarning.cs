namespace TechXpress.Data.Entities;

/// <summary>
/// عمولة الموقع المستحقة على بند طلب معيّن. تُسجَّل مرة واحدة عند تأكيد الدفع،
/// وتُعلَّم كملغاة عند إلغاء الطلب أو إرجاعه.
/// </summary>
public class PlatformEarning
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int OrderDetailId { get; set; }
    public OrderDetail OrderDetail { get; set; } = null!;

    /// <summary>قيمة عمولة الموقع على هذا البند.</summary>
    public decimal Amount { get; set; }

    /// <summary>نسبة العمولة المطبقة وقت العملية (مثال: 0.10).</summary>
    public decimal Rate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsReversed { get; set; }
    public DateTime? ReversedAt { get; set; }
}
