using TechXpress.Data.Entities;
using TechXpress.Data.Repositories.Interfaces;

namespace TechXpress.Data.UoW;

public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    ICategoryRepository Categories { get; }
    IGenericRepository<Order> Orders { get; }
    IGenericRepository<OrderDetail> OrderDetails { get; }

    Task<int> SaveChangesAsync();

    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}