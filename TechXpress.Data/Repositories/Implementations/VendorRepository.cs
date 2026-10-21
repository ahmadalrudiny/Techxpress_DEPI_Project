using Microsoft.EntityFrameworkCore;
using TechXpress.Data.Context;
using TechXpress.Data.Entities;
using TechXpress.Data.Repositories.Interfaces;

namespace TechXpress.Data.Repositories.Implementations;

public class VendorRepository : GenericRepository<Vendor>, IVendorRepository
{
    public VendorRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Vendor?> GetByUserIdAsync(string userId)
        => await _dbSet.FirstOrDefaultAsync(v => v.UserId == userId);

    public async Task<Vendor?> GetByIdWithUserAsync(int id)
        => await _dbSet.Include(v => v.User).FirstOrDefaultAsync(v => v.Id == id);

    public async Task<IEnumerable<Vendor>> GetPendingVendorsAsync()
        => await GetByStatusAsync(VendorStatus.Pending);

    public async Task<IEnumerable<Vendor>> GetByStatusAsync(VendorStatus status)
        => await _dbSet.AsNoTracking().Include(v => v.User)
            .Where(v => v.Status == status)
            .OrderByDescending(v => v.SubmittedAt)
            .ToListAsync();

    public async Task<IEnumerable<Vendor>> GetAllWithUsersAsync()
        => await _dbSet.AsNoTracking().Include(v => v.User)
            .OrderByDescending(v => v.SubmittedAt)
            .ToListAsync();
}