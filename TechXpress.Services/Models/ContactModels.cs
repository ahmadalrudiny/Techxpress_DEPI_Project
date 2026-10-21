namespace TechXpress.Services.Models;

/// <summary>رسالة تواصل جاهزة للعرض في لوحة تحكم الأدمن.</summary>
public record ContactMessageDto(
    int Id,
    string FullName,
    string Email,
    string? Phone,
    string Subject,
    string Message,
    DateTime CreatedAt,
    bool IsRead);
