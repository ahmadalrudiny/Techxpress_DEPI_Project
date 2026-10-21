namespace TechXpress.Services.Interfaces;

/// <summary>
/// يحفظ سلة التسوق الخاصة بالمستخدم في قاعدة البيانات حتى تبقى بعد إغلاق المتصفح
/// أو تسجيل الدخول من جهاز آخر. كل العمليات أفضل-جهد (best effort) ولا تُفشل المتجر.
/// </summary>
public interface ICartPersistenceService
{
    Task<string?> GetSavedCartAsync(string userId);

    Task SaveCartAsync(string userId, string cartJson);

    Task ClearAsync(string userId);
}
