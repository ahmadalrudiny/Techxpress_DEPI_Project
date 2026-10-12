using TechXpress.Data.Entities;

namespace TechXpress.Data.Repositories.Interfaces;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<IEnumerable<Product>> GetAllWithCategoryAsync();
    Task<Product?> GetByIdWithCategoryAsync(int id);
    Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId);
}