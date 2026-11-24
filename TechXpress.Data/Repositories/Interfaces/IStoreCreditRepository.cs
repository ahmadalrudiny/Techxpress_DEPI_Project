using TechXpress.Data.Entities;

namespace TechXpress.Data.Repositories.Interfaces;

/// <summary>الوصول إلى رصيد النقاط وحركاته الخاصة بالعملاء.</summary>
public interface IStoreCreditRepository
{
    /// <summary>الرصيد الحالي للعميل (0 لو المستخدم غير موجود).</summary>
    Task<decimal> GetBalanceAsync(string userId);

    /// <summary>تحميل المستخدم لتعديل رصيده داخل نفس معاملة قاعدة البيانات.</summary>
    Task<ApplicationUser?> GetUserAsync(string userId);

    /// <summary>آخر حركات الرصيد للعميل، الأحدث أولاً.</summary>
    Task<IReadOnlyList<StoreCreditTransaction>> GetTransactionsAsync(string userId, int take = 100);

    /// <summary>هل تم استرداد قيمة هذا الطلب كنقاط بالفعل؟ (لمنع التكرار)</summary>
    Task<bool> HasRefundForOrderAsync(int orderId);

    Task AddTransactionAsync(StoreCreditTransaction transaction);
}
