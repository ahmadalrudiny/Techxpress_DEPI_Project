namespace TechXpress.Services.Models;

public record WishlistItemDto(
    int WishlistItemId,
    int ProductId,
    string ProductName,
    string? ImageUrl,
    string CategoryName,
    decimal Price,
    int StockQuantity,
    DateTime DateAdded,
    bool IsAvailable);

public record WishlistDto(IEnumerable<WishlistItemDto> Items)
{
    public int TotalCount => Items.Count();
    public bool IsEmpty => !Items.Any();
}
