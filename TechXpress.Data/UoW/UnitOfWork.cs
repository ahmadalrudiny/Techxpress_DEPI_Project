using Microsoft.EntityFrameworkCore.Storage;
using TechXpress.Data.Context;
using TechXpress.Data.Entities;
using TechXpress.Data.Repositories.Implementations;
using TechXpress.Data.Repositories.Interfaces;

namespace TechXpress.Data.UoW;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    public IProductRepository Products { get; }
    public ICategoryRepository Categories { get; }
    public IOrderRepository Orders { get; }
    public IGenericRepository<OrderDetail> OrderDetails { get; }
    public IVendorRepository Vendors { get; }
    public IWalletRepository Wallets { get; }
    public IWishlistRepository Wishlists { get; }
    public IReviewRepository Reviews { get; }
    public IGenericRepository<ContactMessage> ContactMessages { get; }
    public IPersistentCartRepository PersistentCarts { get; }
    public IStoreCreditRepository StoreCredits { get; }
    public IGenericRepository<PlatformEarning> PlatformEarnings { get; }
    public IGenericRepository<PlatformExpense> PlatformExpenses { get; }

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;

        Products = new ProductRepository(context);
        Categories = new CategoryRepository(context);
        Orders = new OrderRepository(context);
        OrderDetails = new GenericRepository<OrderDetail>(context);
        Vendors = new VendorRepository(context);
        Wallets = new WalletRepository(context);
        Wishlists = new WishlistRepository(context);
        Reviews = new ReviewRepository(context);
        ContactMessages = new GenericRepository<ContactMessage>(context);
        PersistentCarts = new PersistentCartRepository(context);
        StoreCredits = new StoreCreditRepository(context);
        PlatformEarnings = new GenericRepository<PlatformEarning>(context);
        PlatformExpenses = new GenericRepository<PlatformExpense>(context);
    }

    public async Task<int> SaveChangesAsync()
        => await _context.SaveChangesAsync();

    public async Task BeginTransactionAsync()
    {
        _transaction ??= await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        if (_transaction == null) return;

        try
        {
            await _context.SaveChangesAsync();
            await _transaction.CommitAsync();
        }
        catch
        {
            await RollbackAsync();
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackAsync()
    {
        if (_transaction == null) return;

        await _transaction.RollbackAsync();
        await DisposeTransactionAsync();
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}