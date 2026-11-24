using Microsoft.EntityFrameworkCore;
using TechXpress.Data.Context;
using TechXpress.Data.Entities;
using TechXpress.Data.Repositories.Interfaces;

namespace TechXpress.Data.Repositories.Implementations;

public class WalletRepository : GenericRepository<WalletTransaction>, IWalletRepository
{
    public WalletRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<WalletTransaction>> GetTransactionsByVendorIdAsync(int vendorId)
        => await _dbSet.AsNoTracking()
            .Where(t => t.VendorId == vendorId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<WalletTransaction>> GetPendingPayoutRequestsAsync()
        => await _dbSet.AsNoTracking().Include(t => t.Vendor)
            .Where(t => t.Type == WalletTransactionType.Withdrawal
                     && t.Status == WalletTransactionStatus.Pending)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<WalletTransaction>> GetPayoutRequestsAsync(WalletTransactionStatus? status)
    {
        var query = _dbSet.AsNoTracking().Include(t => t.Vendor)
            .Where(t => t.Type == WalletTransactionType.Withdrawal);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task<(int Total, int Pending, int Approved, int Rejected)> GetPayoutRequestCountsAsync()
    {
        var grouped = await _dbSet.AsNoTracking()
            .Where(t => t.Type == WalletTransactionType.Withdrawal)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        int Get(WalletTransactionStatus s) => grouped.FirstOrDefault(g => g.Status == s)?.Count ?? 0;

        return (grouped.Sum(g => g.Count), Get(WalletTransactionStatus.Pending),
                Get(WalletTransactionStatus.Completed), Get(WalletTransactionStatus.Rejected));
    }

    public async Task<WalletTransaction?> GetByIdWithVendorAsync(int id)
        => await _dbSet.Include(t => t.Vendor).FirstOrDefaultAsync(t => t.Id == id);

    public async Task<bool> HasDepositForOrderDetailAsync(int orderDetailId)
        => await _dbSet.AnyAsync(t => t.OrderDetailId == orderDetailId
                                   && t.Type == WalletTransactionType.Deposit);

    public async Task<bool> HasRefundForOrderDetailAsync(int orderDetailId)
        => await _dbSet.AnyAsync(t => t.OrderDetailId == orderDetailId
                                   && t.Type == WalletTransactionType.Refund);
}