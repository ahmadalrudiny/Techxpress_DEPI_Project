namespace TechXpress.Data.Entities;

/// <summary>رسالة تواصل يرسلها زائر أو عميل من صفحة «تواصل معنا».</summary>
public class ContactMessage
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string Subject { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>هل اطّلع عليها الأدمن؟</summary>
    public bool IsRead { get; set; }
}
