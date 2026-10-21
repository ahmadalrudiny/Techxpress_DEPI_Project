using TechXpress.Services.Models;

namespace TechXpress.Services.Interfaces;

public interface IReviewService
{
    /// <summary>
    /// Gets the reviews of a product plus the average rating and whether the current
    /// user is allowed to add a review (verified purchase, not already reviewed).
    /// </summary>
    Task<ProductReviewsDto> GetProductReviewsAsync(int productId, string? currentUserId);

    /// <summary>Average rating of a product (0 when there are no reviews).</summary>
    Task<double> GetAverageRatingAsync(int productId);

    /// <summary>Creates a verified review (only for products the customer purchased).</summary>
    Task<(bool Success, string Message)> CreateReviewAsync(string userId, CreateReviewRequest request);
}
