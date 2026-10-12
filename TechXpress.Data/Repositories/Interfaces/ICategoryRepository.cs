using TechXpress.Data.Entities;

namespace TechXpress.Data.Repositories.Interfaces;

public interface ICategoryRepository : IGenericRepository<Category>
{
    Task<IEnumerable<Category>> GetAllWithProductsAsync();
}