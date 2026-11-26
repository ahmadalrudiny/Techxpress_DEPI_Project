namespace TechXpress.Data.Entities;

/// <summary>
/// مصروف تشغيلي على المنصة (استضافة، تسويق، عمليات...). يُستخدم في لوحة الإدارة
/// لحساب صافي الربح بعد خصم المصروفات من عمولة المنصة.
/// </summary>
public class PlatformExpense
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    /// <summary>تصنيف المصروف، مثال: استضافة، تسويق، شحن، أخرى.</summary>
    public string Category { get; set; } = null!;

    public decimal Amount { get; set; }

    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>معرّف المدير الذي سجّل المصروف (اختياري).</summary>
    public string? CreatedByUserId { get; set; }
}
