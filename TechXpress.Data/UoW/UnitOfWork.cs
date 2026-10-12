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

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;

        Products = new ProductRepository(context);
        Categories = new CategoryRepository(context);
        Orders = new OrderRepository(context);
        OrderDetails = new GenericRepository<OrderDetail>(context);
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