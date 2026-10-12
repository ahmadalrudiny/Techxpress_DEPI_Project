using Microsoft.Data.Sqlite;
using TechXpress.Data.Context;
using TechXpress.Data.Entities;
using TechXpress.Data.Repositories.Implementations;

namespace TechXpress.Data.Tests;

public class ProductRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SqliteConnection _connection;

    public ProductRepositoryTests()
    {
        (_context, _connection) = TestDbFactory.Create();

        var laptops = new Category { Name = "Laptops" };
        var mobiles = new Category { Name = "Mobiles" };
        _context.Products.AddRange(
            new Product { Name = "Dell XPS", Price = 1000, StockQuantity = 5, Category = laptops },
            new Product { Name = "HP Pavilion", Price = 700, StockQuantity = 3, Category = laptops },
            new Product { Name = "iPhone", Price = 800, StockQuantity = 9, Category = mobiles });
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByCategoryAsync_ReturnsOnlyThatCategory()
    {
        var repo = new ProductRepository(_context);
        var laptops = _context.Categories.First(c => c.Name == "Laptops");

        var result = await repo.GetByCategoryAsync(laptops.Id);

        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Equal(laptops.Id, p.CategoryId));
    }

    [Fact]
    public async Task GetAllWithCategoryAsync_LoadsCategory()
    {
        var repo = new ProductRepository(_context);

        var result = await repo.GetAllWithCategoryAsync();

        Assert.Equal(3, result.Count());
        Assert.All(result, p => Assert.NotNull(p.Category));
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}