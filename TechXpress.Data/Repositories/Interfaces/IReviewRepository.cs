using TechXpress.Data.Entities;

namespace TechXpress.Data.Repositories.Interfaces;

public interface IReviewRepository : IGenericRepository<Review>
{
    /// <summary>كل مراجعات منتج مع بيانات العميل، الأحدث أولاً.</summary>
    Task<List<Review>> GetByProductIdAsync(int productId);

    /// <summary>مراجعة عميل محدد لمنتج محدد، أو null.</summary>
    Task<Review?> GetByUserAndProductAsync(string userId, int productId);

    /// <summary>هل اشترى هذا العميل هذا المنتج فعلياً؟</summary>
    Task<bool> HasUserPurchasedProductAsync(string userId, int productId);

    /// <summary>متوسط تقييم منتج (0 لو لا توجد مراجعات).</summary>
    Task<double> GetAverageRatingAsync(int productId);
}
