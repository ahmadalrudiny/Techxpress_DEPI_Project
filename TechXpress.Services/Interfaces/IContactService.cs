using TechXpress.Services.Models;

namespace TechXpress.Services.Interfaces;

/// <summary>
/// خدمة رسائل «تواصل معنا»: حفظ رسالة جديدة من الزائر، وعرضها وإدارتها من لوحة الأدمن.
/// </summary>
public interface IContactService
{
    /// <summary>يحفظ رسالة تواصل جديدة بعد التحقق من المدخلات.</summary>
    /// <returns>نجاح العملية مع رسالة عربية مناسبة للعرض للمستخدم.</returns>
    Task<(bool Success, string Message)> SendAsync(
        string fullName, string email, string? phone, string subject, string message);

    /// <summary>يُرجع كل رسائل التواصل مرتبة من الأحدث للأقدم.</summary>
    Task<IReadOnlyList<ContactMessageDto>> GetAllAsync();

    /// <summary>عدد الرسائل غير المقروءة (لشارة التنبيه في لوحة الأدمن).</summary>
    Task<int> GetUnreadCountAsync();

    /// <summary>يُعلّم رسالة كمقروءة.</summary>
    Task<(bool Success, string Message)> MarkAsReadAsync(int id);

    /// <summary>يحذف رسالة تواصل.</summary>
    Task<(bool Success, string Message)> DeleteAsync(int id);
}
