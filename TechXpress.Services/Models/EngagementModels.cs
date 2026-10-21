namespace TechXpress.Services.Models;

public record ReviewDto(
    int Id,
    int Rating,
    string Comment,
    string CustomerName,
    DateTime CreatedAt);

public record CreateReviewRequest(int ProductId, int Rating, string Comment);

public record ProductReviewsDto(
    double AverageRating,
    int TotalReviews,
    IEnumerable<ReviewDto> Reviews,
    bool CanReview,
    bool HasReviewed);

public record AdminUserDto(
    string UserId,
    string FullName,
    string Email,
    string? PhoneNumber,
    string? City,
    DateTime CreatedAt,
    bool IsActive,
    IReadOnlyList<string> Roles);
