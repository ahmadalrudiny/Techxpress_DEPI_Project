using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TechXpress.Data.Context;
using TechXpress.Data.Entities;
using TechXpress.Data.UoW;
using TechXpress.Services.Implementations;

namespace TechXpress.Services.Tests;

/// <summary>اختبارات سلة التسوق المحفوظة في قاعدة البيانات.</summary>
public class CartPersistenceServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _uow;
    private readonly CartPersistenceService _service;
    private readonly string _userId;

    public CartPersistenceServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _uow = new UnitOfWork(_context);
        _service = new CartPersistenceService(_uow, Mock.Of<ILogger<CartPersistenceService>>());

        _userId = Guid.NewGuid().ToString();
        _context.Users.Add(new ApplicationUser
        {
            Id = _userId,
            UserName = "cart-user@test.com",
            NormalizedUserName = "CART-USER@TEST.COM",
            Email = "cart-user@test.com",
            NormalizedEmail = "CART-USER@TEST.COM",
            FullName = "عميل سلة"
        });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private async Task<string?> ReadStoredCartAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == _userId)
            .Select(u => u.CartJson)
            .FirstOrDefaultAsync();
    }

    [Fact]
    public async Task SaveCartAsync_ValidJson_PersistsToDatabase()
    {
        const string json = "[{\"productId\":1,\"quantity\":2}]";

        await _service.SaveCartAsync(_userId, json);

        Assert.Equal(json, await ReadStoredCartAsync());
    }

    [Fact]
    public async Task GetSavedCartAsync_WhenCartExists_ReturnsJson()
    {
        const string json = "[{\"productId\":5,\"quantity\":1}]";
        await _service.SaveCartAsync(_userId, json);

        var result = await _service.GetSavedCartAsync(_userId);

        Assert.Equal(json, result);
    }

    [Fact]
    public async Task GetSavedCartAsync_WhenNoCart_ReturnsNull()
    {
        var result = await _service.GetSavedCartAsync(_userId);

        Assert.Null(result);
    }

    [Fact]
    public async Task ClearAsync_RemovesStoredCart()
    {
        await _service.SaveCartAsync(_userId, "[{\"productId\":1,\"quantity\":1}]");

        await _service.ClearAsync(_userId);

        Assert.Null(await ReadStoredCartAsync());
    }

    [Fact]
    public async Task SaveCartAsync_EmptyCart_ClearsStoredCart()
    {
        await _service.SaveCartAsync(_userId, "[{\"productId\":1,\"quantity\":1}]");

        await _service.SaveCartAsync(_userId, "[]");

        Assert.Null(await ReadStoredCartAsync());
    }

    [Fact]
    public async Task SaveCartAsync_UnknownUser_DoesNotThrowAndWritesNothing()
    {
        await _service.SaveCartAsync(Guid.NewGuid().ToString(), "[]");

        Assert.Null(await ReadStoredCartAsync());
    }

    [Fact]
    public async Task GetSavedCartAsync_EmptyUserId_ReturnsNull()
    {
        var result = await _service.GetSavedCartAsync("");

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveCartAsync_CartLargerThanLimit_IsIgnored()
    {
        var huge = new string('x', 200_001);

        await _service.SaveCartAsync(_userId, huge);

        Assert.Null(await ReadStoredCartAsync());
    }
}
