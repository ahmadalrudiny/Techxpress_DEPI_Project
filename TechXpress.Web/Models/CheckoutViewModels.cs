namespace TechXpress.Web.Models;

public class CheckoutViewModel
{
    public string ShippingAddress { get; set; } = string.Empty;
    public string? PromoCode { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
}
