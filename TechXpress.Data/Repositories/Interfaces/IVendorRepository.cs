using TechXpress.Data.Entities;

namespace TechXpress.Data.Repositories.Interfaces;

public interface IVendorRepository : IGenericRepository<Vendor>
{
    Task<Vendor?> GetByUserIdAsync(string userId);
    Task<Vendor?> GetByIdWithUserAsync(int id);
    Task<IEnumerable<Vendor>> GetPendingVendorsAsync();
    Task<IEnumerable<Vendor>> GetByStatusAsync(VendorStatus status);
    Task<IEnumerable<Vendor>> GetAllWithUsersAsync();
}