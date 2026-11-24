using Microsoft.AspNetCore.Identity;

namespace TechXpress.Data.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = null!;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }

    /// <summary>عند التعليق (false) يُمنع المستخدم من تسجيل الدخول.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>سلة التسوق المحفوظة (JSON) — تُستعاد عند تسجيل الدخول من جهاز آخر.</summary>
    public string? CartJson { get; set; }

    /// <summary>
    /// رصيد النقاط/المكافآت المتاح للعميل (1 نقطة = 1 دولار).
    /// يُضاف عند إلغاء أو إرجاع طلب مدفوع، ويُستخدم كخصم عند الشراء.
    /// </summary>
    public decimal StoreCreditBalance { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
