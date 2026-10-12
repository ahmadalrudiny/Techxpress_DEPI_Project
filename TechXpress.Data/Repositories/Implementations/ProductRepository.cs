using Microsoft.EntityFrameworkCore;
using TechXpress.Data.Context;
using TechXpress.Data.Entities;
using TechXpress.Data.Repositories.Interfaces;

namespace TechXpress.Data.Repositories.Implementations;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Product>> GetAllWithCategoryAsync()
        => await _dbSet.Include(p => p.Category).ToListAsync();

    public async Task<Product?> GetByIdWithCategoryAsync(int id)
        => await _dbSet.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId)
        => await _dbSet.Include(p => p.Category)
                       .Where(p => p.CategoryId == categoryId)
                       .ToListAsync();
}