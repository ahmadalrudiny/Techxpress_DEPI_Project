using Microsoft.EntityFrameworkCore;
using TechXpress.Data.Context;
using TechXpress.Data.Entities;
using TechXpress.Data.Repositories.Interfaces;

namespace TechXpress.Data.Repositories.Implementations;

public class StoreCreditRepository : IStoreCreditRepository
{
    private readonly ApplicationDbContext _context;

    public StoreCreditRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> GetBalanceAsync(string userId)
        => await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.StoreCreditBalance)
            .FirstOrDefaultAsync();

    public async Task<ApplicationUser?> GetUserAsync(string userId)
        => await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

    public async Task<IReadOnlyList<StoreCreditTransaction>> GetTransactionsAsync(string userId, int take = 100)
        => await _context.StoreCreditTransactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Take(take)
            .ToListAsync();

    public Task<bool> HasRefundForOrderAsync(int orderId)
        => _context.StoreCreditTransactions
            .AnyAsync(t => t.OrderId == orderId
                        && t.Type == StoreCreditTransactionType.OrderRefund);

    public async Task AddTransactionAsync(StoreCreditTransaction transaction)
        => await _context.StoreCreditTransactions.AddAsync(transaction);
}
