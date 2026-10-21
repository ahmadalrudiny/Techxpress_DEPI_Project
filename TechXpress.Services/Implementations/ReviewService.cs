using Microsoft.Extensions.Logging;
using TechXpress.Data.Entities;
using TechXpress.Data.UoW;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;

namespace TechXpress.Services.Implementations;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(IUnitOfWork unitOfWork, ILogger<ReviewService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ProductReviewsDto> GetProductReviewsAsync(int productId, string? currentUserId)
    {
        var reviews = await _unitOfWork.Reviews.GetByProductIdAsync(productId);

        var dtos = reviews.Select(r => new ReviewDto(
            r.Id,
            r.Rating,
            r.Comment,
            string.IsNullOrWhiteSpace(r.User?.FullName) ? "عميل" : r.User!.FullName,
            r.CreatedAt)).ToList();

        var average = dtos.Count == 0 ? 0.0 : Math.Round(dtos.Average(r => r.Rating), 1);

        bool hasReviewed = false;
        bool canReview = false;

        if (!string.IsNullOrEmpty(currentUserId))
        {
            hasReviewed = reviews.Any(r => r.UserId == currentUserId);

            if (!hasReviewed)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(productId);
                if (product != null && product.IsActive)
                {
                    canReview = await _unitOfWork.Reviews
                        .HasUserPurchasedProductAsync(currentUserId, productId);
                }
            }
        }

        return new ProductReviewsDto(average, dtos.Count, dtos, canReview, hasReviewed);
    }

    public async Task<double> GetAverageRatingAsync(int productId)
    {
        var avg = await _unitOfWork.Reviews.GetAverageRatingAsync(productId);
        return Math.Round(avg, 1);
    }

    public async Task<(bool Success, string Message)> CreateReviewAsync(string userId, CreateReviewRequest request)
    {
        if (request.Rating < 1 || request.Rating > 5)
            return (false, "التقييم يجب أن يكون بين 1 و 5 نجوم.");

        var comment = request.Comment?.Trim() ?? string.Empty;
        if (comment.Length < 3)
            return (false, "التعليق يجب أن يكون 3 أحرف على الأقل.");

        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId);
        if (product == null || !product.IsActive)
            return (false, "هذا المنتج غير متاح للمراجعة.");

        var purchased = await _unitOfWork.Reviews
            .HasUserPurchasedProductAsync(userId, request.ProductId);
        if (!purchased)
            return (false, "يمكنك تقييم المنتجات التي اشتريتها فقط.");

        var existing = await _unitOfWork.Reviews.GetByUserAndProductAsync(userId, request.ProductId);
        if (existing != null)
            return (false, "لقد قمت بتقييم هذا المنتج بالفعل.");

        await _unitOfWork.Reviews.AddAsync(new Review
        {
            ProductId = request.ProductId,
            UserId = userId,
            Rating = request.Rating,
            Comment = comment.Length > 1000 ? comment[..1000] : comment,
            CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User {UserId} reviewed product {ProductId} with {Rating} stars",
            userId, request.ProductId, request.Rating);

        return (true, "شكراً لك! تم إرسال تقييمك بنجاح.");
    }
}
