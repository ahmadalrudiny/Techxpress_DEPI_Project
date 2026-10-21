using TechXpress.Data.Entities;

namespace TechXpress.Data.Repositories.Interfaces;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<IEnumerable<Product>> GetAllWithCategoryAsync();
    Task<Product?> GetByIdWithCategoryAsync(int id);
    Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId);

    /// <summary>كل منتجات بائع معيّن (للإحصائيات ولوحة تحكم البائع).</summary>
    Task<IEnumerable<Product>> GetByVendorIdAsync(int vendorId);
}