namespace TechXpress.Data.Repositories.Interfaces;

/// <summary>قراءة/كتابة سلة التسوق المحفوظة للمستخدم (JSON) دون كشف أنواع Identity للخدمات.</summary>
public interface IPersistentCartRepository
{
    Task<string?> GetCartJsonAsync(string userId);

    /// <summary>يحفظ السلة؛ القيمة null أو الفارغة تعني حذف السلة المحفوظة.</summary>
    Task<bool> SaveCartJsonAsync(string userId, string? cartJson);
}
