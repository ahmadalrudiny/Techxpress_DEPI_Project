using TechXpress.Services.Models;

namespace TechXpress.Services.Interfaces;

/// <summary>إدارة رصيد النقاط (Store Credit) الخاص بالعميل.</summary>
public interface IStoreCreditService
{
    /// <summary>الرصيد المتاح حاليًا للعميل.</summary>
    Task<decimal> GetBalanceAsync(string userId);

    /// <summary>ملخص الرصيد مع آخر الحركات، أو null لو المستخدم غير موجود.</summary>
    Task<StoreCreditSummaryDto?> GetSummaryAsync(string userId);

    /// <summary>إضافة رصيد نقاط للعميل (مثل تعويض أو تعديل إداري).</summary>
    Task<(bool Success, string Message)> CreditAsync(
        string userId, decimal amount, string description, int? orderId = null);
}
