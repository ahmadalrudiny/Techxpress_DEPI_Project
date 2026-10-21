using TechXpress.Data.Entities;
using TechXpress.Data.Repositories.Interfaces;

namespace TechXpress.Data.UoW;

public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    ICategoryRepository Categories { get; }
    IOrderRepository Orders { get; }
    IGenericRepository<OrderDetail> OrderDetails { get; }
    IVendorRepository Vendors { get; }
    IWalletRepository Wallets { get; }
    IWishlistRepository Wishlists { get; }
    IReviewRepository Reviews { get; }
    IGenericRepository<ContactMessage> ContactMessages { get; }
    IPersistentCartRepository PersistentCarts { get; }

    Task<int> SaveChangesAsync();

    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}