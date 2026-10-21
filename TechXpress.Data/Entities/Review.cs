namespace TechXpress.Data.Entities;

/// <summary>تقييم منتج من عميل اشترى المنتج فعلياً.</summary>
public class Review
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

    /// <summary>من 1 إلى 5 نجوم.</summary>
    public int Rating { get; set; }

    public string Comment { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
