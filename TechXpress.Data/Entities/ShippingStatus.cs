namespace TechXpress.Data.Entities;

/// <summary>
/// حالة الشحن على مستوى بند الطلب (كل بائع مسؤول عن شحن منتجاته بنفسه).
/// </summary>
public enum ShippingStatus
{
    /// <summary>بانتظار تجهيز البائع.</summary>
    Pending,

    /// <summary>البائع بدأ تجهيز الشحنة.</summary>
    Preparing,

    /// <summary>تم تسليم الشحنة لشركة الشحن.</summary>
    Shipped,

    /// <summary>تم توصيل الشحنة للعميل.</summary>
    Delivered
}
