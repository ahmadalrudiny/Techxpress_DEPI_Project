namespace TechXpress.Data.Entities;

/// <summary>نوع حركة رصيد النقاط الخاص بالعميل.</summary>
public enum StoreCreditTransactionType
{
    /// <summary>إضافة رصيد نتيجة إلغاء أو إرجاع طلب.</summary>
    OrderRefund,

    /// <summary>خصم رصيد عند استخدامه في عملية شراء.</summary>
    OrderPayment,

    /// <summary>تعديل يدوي من الإدارة.</summary>
    AdminAdjustment
}

/// <summary>حركة على رصيد النقاط (Store Credit) الخاص بالعميل.</summary>
public class StoreCreditTransaction
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

    /// <summary>موجب = إضافة رصيد، سالب = خصم رصيد.</summary>
    public decimal Amount { get; set; }

    /// <summary>الرصيد بعد تنفيذ الحركة.</summary>
    public decimal BalanceAfter { get; set; }

    public StoreCreditTransactionType Type { get; set; }

    /// <summary>الطلب المرتبط بالحركة (إن وجد).</summary>
    public int? OrderId { get; set; }
    public Order? Order { get; set; }

    public string Description { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
