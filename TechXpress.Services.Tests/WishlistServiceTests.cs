using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TechXpress.Data.Context;
using TechXpress.Data.Entities;
using TechXpress.Data.UoW;
using TechXpress.Services.Implementations;

namespace TechXpress.Services.Tests;

public class WishlistServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _uow;
    private readonly WishlistService _service;

    public WishlistServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _uow = new UnitOfWork(_context);
        _service = new WishlistService(_uow, Mock.Of<ILogger<WishlistService>>());
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private async Task<(ApplicationUser User, Product Product)> SeedAsync(bool productActive = true, int stock = 5)
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "user1@test.com",
            Email = "user1@test.com",
            FullName = "Test User"
        };
        var category = new Category { Name = "Electronics" };
        _context.Users.Add(user);
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Laptop",
            Price = 1500m,
            StockQuantity = stock,
            CategoryId = category.Id,
            IsActive = productActive
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return (user, product);
    }

    [Fact]
    public async Task AddAsync_ValidProduct_AddsItem()
    {
        var (user, product) = await SeedAsync();

        var (success, message) = await _service.AddAsync(user.Id, product.Id);

        Assert.True(success);
        Assert.Contains("Laptop", message);
        Assert.True(await _service.IsInWishlistAsync(user.Id, product.Id));
    }

    [Fact]
    public async Task AddAsync_DuplicateProduct_ReturnsFalse()
    {
        var (user, product) = await SeedAsync();

        await _service.AddAsync(user.Id, product.Id);
        var (success, message) = await _service.AddAsync(user.Id, product.Id);

        Assert.False(success);
        Assert.Contains("بالفعل", message);
        Assert.Equal(1, await _service.GetWishlistCountAsync(user.Id));
    }

    [Fact]
    public async Task AddAsync_InactiveProduct_ReturnsFalse()
    {
        var (user, product) = await SeedAsync(productActive: false);

        var (success, _) = await _service.AddAsync(user.Id, product.Id);

        Assert.False(success);
        Assert.False(await _service.IsInWishlistAsync(user.Id, product.Id));
    }

    [Fact]
    public async Task GetWishlistAsync_MapsAvailabilityAndCategory()
    {
        var (user, product) = await SeedAsync(stock: 3);
        await _service.AddAsync(user.Id, product.Id);

        var wishlist = await _service.GetWishlistAsync(user.Id);

        Assert.Equal(1, wishlist.TotalCount);
        var item = Assert.Single(wishlist.Items);
        Assert.Equal("Laptop", item.ProductName);
        Assert.Equal("Electronics", item.CategoryName);
        Assert.Equal(1500m, item.Price);
        Assert.True(item.IsAvailable);
    }

    [Fact]
    public async Task GetWishlistAsync_OutOfStock_MarkedUnavailable()
    {
        var (user, product) = await SeedAsync(stock: 0);
        await _service.AddAsync(user.Id, product.Id);

        var wishlist = await _service.GetWishlistAsync(user.Id);

        var item = Assert.Single(wishlist.Items);
        Assert.False(item.IsAvailable);
    }

    [Fact]
    public async Task RemoveAsync_OwnItem_RemovesIt()
    {
        var (user, product) = await SeedAsync();
        await _service.AddAsync(user.Id, product.Id);
        var wishlist = await _service.GetWishlistAsync(user.Id);
        var itemId = wishlist.Items.Single().WishlistItemId;

        var (success, _) = await _service.RemoveAsync(user.Id, itemId);

        Assert.True(success);
        Assert.Equal(0, await _service.GetWishlistCountAsync(user.Id));
    }

    [Fact]
    public async Task RemoveAsync_OtherUsersItem_DoesNotRemove()
    {
        var (user, product) = await SeedAsync();
        await _service.AddAsync(user.Id, product.Id);
        var itemId = (await _service.GetWishlistAsync(user.Id)).Items.Single().WishlistItemId;

        // مستخدم آخر يحاول حذف عنصر لا يملكه
        var (success, _) = await _service.RemoveAsync("other-user", itemId);

        Assert.False(success);
        Assert.Equal(1, await _service.GetWishlistCountAsync(user.Id));
    }

    [Fact]
    public async Task RemoveByProductAsync_RemovesMatchingProduct()
    {
        var (user, product) = await SeedAsync();
        await _service.AddAsync(user.Id, product.Id);

        var (success, _) = await _service.RemoveByProductAsync(user.Id, product.Id);

        Assert.True(success);
        Assert.False(await _service.IsInWishlistAsync(user.Id, product.Id));
    }
}
