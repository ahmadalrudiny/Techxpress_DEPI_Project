using TechXpress.Data.Entities;

namespace TechXpress.Data.Repositories.Interfaces;

public interface IWishlistRepository : IGenericRepository<WishlistItem>
{
    /// <summary>كل عناصر مفضلة مستخدم مع المنتج والفئة.</summary>
    Task<List<WishlistItem>> GetByUserIdAsync(string userId);

    /// <summary>عنصر مفضلة محدد، أو null.</summary>
    Task<WishlistItem?> GetItemAsync(string userId, int productId);

    /// <summary>عنصر مفضلة بالمعرف مع التحقق من ملكيته للمستخدم.</summary>
    Task<WishlistItem?> GetByIdForUserAsync(int wishlistItemId, string userId);
}
