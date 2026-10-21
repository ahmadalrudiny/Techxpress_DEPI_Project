using System.IO;
using TechXpress.Data.Entities;

namespace TechXpress.Services.Models;

public record ProductCreateRequest(
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    int CategoryId,
    UploadedFile? Image);

public record ProductUpdateRequest(
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    int CategoryId,
    bool IsActive,
    UploadedFile? Image);
