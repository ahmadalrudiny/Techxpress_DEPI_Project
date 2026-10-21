namespace TechXpress.Services.Models;

public record ProductDto(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    string? ImageUrl,
    bool IsActive,
    int CategoryId,
    string CategoryName,
    int? VendorId,
    string? VendorStoreName,
    DateTime CreatedAt);
