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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
