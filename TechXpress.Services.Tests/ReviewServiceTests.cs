using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TechXpress.Data.Context;
using TechXpress.Data.Entities;
using TechXpress.Data.UoW;
using TechXpress.Services.Implementations;
using TechXpress.Services.Models;

namespace TechXpress.Services.Tests;

public class ReviewServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _uow;
    private readonly ReviewService _service;

    public ReviewServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _uow = new UnitOfWork(_context);
        _service = new ReviewService(_uow, Mock.Of<ILogger<ReviewService>>());
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private async Task<(ApplicationUser User, Product Product)> SeedAsync(bool withPurchase)
    {
        var user = new ApplicationUser
        {
            Id = "buyer-1",
            UserName = "buyer@test.com",
            Email = "buyer@test.com",
            FullName = "Buyer One"
        };
        var category = new Category { Name = "Audio" };
        _context.Users.Add(user);
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Headphones",
            Price = 200m,
            StockQuantity = 10,
            CategoryId = category.Id,
            IsActive = true
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        if (withPurchase)
        {
            var order = new Order
            {
                UserId = user.Id,
                TotalAmount = 200m,
                Status = OrderStatus.Paid
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            _context.OrderDetails.Add(new OrderDetail
            {
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = 1,
                UnitPrice = 200m
            });
            await _context.SaveChangesAsync();
        }

        return (user, product);
    }

    [Fact]
    public async Task CreateReviewAsync_NotPurchased_ReturnsFalse()
    {
        var (user, product) = await SeedAsync(withPurchase: false);

        var (success, message) = await _service.CreateReviewAsync(
            user.Id, new CreateReviewRequest(product.Id, 5, "منتج رائع جداً"));

        Assert.False(success);
        Assert.Contains("اشتريت", message);
    }

    [Fact]
    public async Task CreateReviewAsync_Purchased_Succeeds()
    {
        var (user, product) = await SeedAsync(withPurchase: true);

        var (success, _) = await _service.CreateReviewAsync(
            user.Id, new CreateReviewRequest(product.Id, 5, "منتج رائع جداً"));

        Assert.True(success);
        Assert.Equal(5.0, await _service.GetAverageRatingAsync(product.Id));
    }

    [Fact]
    public async Task CreateReviewAsync_CancelledOrder_DoesNotCountAsPurchase()
    {
        var (user, product) = await SeedAsync(withPurchase: true);
        var order = await _context.Orders.FirstAsync();
        order.Status = OrderStatus.Cancelled;
        await _context.SaveChangesAsync();

        var (success, _) = await _service.CreateReviewAsync(
            user.Id, new CreateReviewRequest(product.Id, 4, "لم يعجبني"));

        Assert.False(success);
    }

    [Fact]
    public async Task CreateReviewAsync_Duplicate_ReturnsFalse()
    {
        var (user, product) = await SeedAsync(withPurchase: true);
        await _service.CreateReviewAsync(user.Id, new CreateReviewRequest(product.Id, 5, "ممتاز جداً"));

        var (success, message) = await _service.CreateReviewAsync(
            user.Id, new CreateReviewRequest(product.Id, 3, "تقييم ثانٍ"));

        Assert.False(success);
        Assert.Contains("بالفعل", message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task CreateReviewAsync_InvalidRating_ReturnsFalse(int rating)
    {
        var (user, product) = await SeedAsync(withPurchase: true);

        var (success, _) = await _service.CreateReviewAsync(
            user.Id, new CreateReviewRequest(product.Id, rating, "تقييم غير صالح"));

        Assert.False(success);
    }

    [Fact]
    public async Task CreateReviewAsync_ShortComment_ReturnsFalse()
    {
        var (user, product) = await SeedAsync(withPurchase: true);

        var (success, _) = await _service.CreateReviewAsync(
            user.Id, new CreateReviewRequest(product.Id, 5, "ab"));

        Assert.False(success);
    }

    [Fact]
    public async Task GetProductReviewsAsync_ComputesAverageAndCanReview()
    {
        var (user, product) = await SeedAsync(withPurchase: true);

        // قبل المراجعة: مستخدم مشترٍ لم يقيّم بعد
        var before = await _service.GetProductReviewsAsync(product.Id, user.Id);
        Assert.True(before.CanReview);
        Assert.False(before.HasReviewed);
        Assert.Equal(0, before.TotalReviews);

        await _service.CreateReviewAsync(user.Id, new CreateReviewRequest(product.Id, 4, "جيد جداً"));

        var after = await _service.GetProductReviewsAsync(product.Id, user.Id);
        Assert.False(after.CanReview);
        Assert.True(after.HasReviewed);
        Assert.Equal(1, after.TotalReviews);
        Assert.Equal(4.0, after.AverageRating);
        Assert.Equal("Buyer One", after.Reviews.Single().CustomerName);
    }

    [Fact]
    public async Task GetProductReviewsAsync_AnonymousUser_CannotReview()
    {
        var (_, product) = await SeedAsync(withPurchase: true);

        var result = await _service.GetProductReviewsAsync(product.Id, null);

        Assert.False(result.CanReview);
        Assert.False(result.HasReviewed);
    }
}
