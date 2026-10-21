using Microsoft.EntityFrameworkCore;
using TechXpress.Data.Context;
using TechXpress.Data.Entities;
using TechXpress.Data.Repositories.Interfaces;

namespace TechXpress.Data.Repositories.Implementations;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Order?> GetByIdWithDetailsAsync(int id)
        => await _dbSet
            .Include(o => o.User)
            .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
            .Include(o => o.OrderDetails).ThenInclude(d => d.Vendor)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<Order?> GetByIdForUserAsync(int id, string userId)
        => await _dbSet
            .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
            .Include(o => o.OrderDetails).ThenInclude(d => d.Vendor)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

    public async Task<IEnumerable<Order>> GetByUserIdAsync(string userId)
        => await _dbSet
            .Where(o => o.UserId == userId)
            .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
            .Include(o => o.OrderDetails).ThenInclude(d => d.Vendor)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

    public async Task<IEnumerable<Order>> GetAllWithUsersAsync()
        => await _dbSet
            .Include(o => o.User)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

    public async Task<IEnumerable<OrderDetail>> GetOrderDetailsByVendorIdAsync(int vendorId)
        => await _dbSet
            .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
            .SelectMany(o => o.OrderDetails.Where(d => d.VendorId == vendorId))
            .ToListAsync();

    public async Task<IEnumerable<Order>> GetOrdersByVendorIdAsync(int vendorId)
        => await _dbSet
            .AsNoTracking()
            .Where(o => o.OrderDetails.Any(d => d.VendorId == vendorId))
            .Include(o => o.OrderDetails.Where(d => d.VendorId == vendorId))
                .ThenInclude(d => d.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

    public async Task<Order?> GetOrderForVendorAsync(int orderId, int vendorId)
        => await _dbSet
            .AsNoTracking()
            .Where(o => o.Id == orderId && o.OrderDetails.Any(d => d.VendorId == vendorId))
            .Include(o => o.User)
            .Include(o => o.OrderDetails.Where(d => d.VendorId == vendorId))
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync();
}