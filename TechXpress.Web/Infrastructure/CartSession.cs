namespace TechXpress.Web.Infrastructure;

/// <summary>
/// مفاتيح وقيم سلة التسوق المخزّنة في الجلسة، مشتركة بين المتحكمات والتخطيطات
/// حتى لا تتكرر النصوص الحرفية في أكثر من مكان.
/// </summary>
public static class CartSession
{
    /// <summary>مفتاح سلة التسوق في الجلسة.</summary>
    public const string Key = "Cart";

    /// <summary>تمثيل السلة الفارغة (JSON).</summary>
    public const string EmptyJson = "[]";
}
