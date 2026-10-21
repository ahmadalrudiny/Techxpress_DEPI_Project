namespace TechXpress.Data.Entities;

/// <summary>عنصر في قائمة مفضلات العميل.</summary>
public class WishlistItem
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
}
