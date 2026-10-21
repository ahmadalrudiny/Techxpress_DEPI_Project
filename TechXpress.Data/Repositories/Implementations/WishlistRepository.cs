using Microsoft.EntityFrameworkCore;
using TechXpress.Data.Context;
using TechXpress.Data.Entities;
using TechXpress.Data.Repositories.Interfaces;

namespace TechXpress.Data.Repositories.Implementations;

public class WishlistRepository : GenericRepository<WishlistItem>, IWishlistRepository
{
    public WishlistRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<WishlistItem>> GetByUserIdAsync(string userId)
        => await _dbSet
            .AsNoTracking()
            .Include(w => w.Product).ThenInclude(p => p.Category)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.DateAdded)
            .ToListAsync();

    public async Task<WishlistItem?> GetItemAsync(string userId, int productId)
        => await _dbSet
            .Include(w => w.Product).ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

    public async Task<WishlistItem?> GetByIdForUserAsync(int wishlistItemId, string userId)
        => await _dbSet
            .Include(w => w.Product)
            .FirstOrDefaultAsync(w => w.Id == wishlistItemId && w.UserId == userId);
}
