using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechXpress.Data.Context;
using TechXpress.Data.Entities;
using TechXpress.Data.UoW;

namespace TechXpress.Data.Tests;

public class UnitOfWorkTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SqliteConnection _connection;
    private readonly UnitOfWork _uow;

    public UnitOfWorkTests()
    {
        (_context, _connection) = TestDbFactory.Create();
        _uow = new UnitOfWork(_context);
    }

    [Fact]
    public async Task Commit_SavesChanges()
    {
        await _uow.BeginTransactionAsync();
        await _uow.Categories.AddAsync(new Category { Name = "Laptops" });
        await _uow.CommitAsync();

        var count = await _context.Categories.AsNoTracking().CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Rollback_DoesNotSaveChanges()
    {
        await _uow.BeginTransactionAsync();
        await _uow.Categories.AddAsync(new Category { Name = "Laptops" });
        await _uow.SaveChangesAsync();   // اتحفظت داخل الـ Transaction بس
        await _uow.RollbackAsync();

        var count = await _context.Categories.AsNoTracking().CountAsync();
        Assert.Equal(0, count);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}