using Microsoft.EntityFrameworkCore;
using TechXpress.Data.Context;
using TechXpress.Data.Entities;
using TechXpress.Data.Repositories.Interfaces;

namespace TechXpress.Data.Repositories.Implementations;

public class ReviewRepository : GenericRepository<Review>, IReviewRepository
{
    public ReviewRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<Review>> GetByProductIdAsync(int productId)
        => await _dbSet
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<Review?> GetByUserAndProductAsync(string userId, int productId)
        => await _dbSet
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == productId);

    public async Task<bool> HasUserPurchasedProductAsync(string userId, int productId)
        => await _context.Orders
            .AsNoTracking()
            .AnyAsync(o => o.UserId == userId
                        && o.Status != OrderStatus.Cancelled
                        && o.OrderDetails.Any(d => d.ProductId == productId));

    public async Task<double> GetAverageRatingAsync(int productId)
    {
        var ratings = await _dbSet
            .AsNoTracking()
            .Where(r => r.ProductId == productId)
            .Select(r => r.Rating)
            .ToListAsync();

        return ratings.Count == 0 ? 0.0 : ratings.Average();
    }
}
