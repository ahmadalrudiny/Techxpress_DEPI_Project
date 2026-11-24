namespace TechXpress.Web.Models;

public class CheckoutViewModel
{
    public string ShippingAddress { get; set; } = string.Empty;
    public string? PromoCode { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }

    /// <summary>رصيد النقاط المتاح للعميل.</summary>
    public decimal AvailableCredit { get; set; }

    /// <summary>هل يريد العميل استخدام رصيد النقاط في هذا الطلب؟</summary>
    public bool UseStoreCredit { get; set; }
}
