using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechXpress.Data.Context;
using TechXpress.Data.Entities;
using TechXpress.Data.UoW;
using TechXpress.Services.Implementations;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;

namespace TechXpress.Services.Tests;

public class ProductServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SqliteConnection _connection;
    private readonly IUnitOfWork _uow;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _uow = new UnitOfWork(_context);
        _service = new ProductService(_uow, new Logger<ProductService>(new LoggerFactory()));

        // Seed data
        var cats = new List<Category>
        {
            new Category { Id = 1, Name = "Laptops" },
            new Category { Id = 2, Name = "Mobiles" }
        };
        _context.Categories.AddRange(cats);

        _context.Products.AddRange(
            new Product { Id = 1, Name = "Dell XPS", Price = 1000m, StockQuantity = 5, CategoryId = 1, VendorId = null, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Product { Id = 2, Name = "HP Pavilion", Price = 700m, StockQuantity = 3, CategoryId = 1, VendorId = null, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new Product { Id = 3, Name = "iPhone 15", Price = 800m, StockQuantity = 9, CategoryId = 2, VendorId = null, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Product { Id = 4, Name = "Old Item", Price = 100m, StockQuantity = 0, CategoryId = 1, VendorId = null, IsActive = false, CreatedAt = DateTime.UtcNow.AddDays(-30) }
        );
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsOnlyActiveProducts()
    {
        var (items, total) = await _service.GetPagedAsync();

        Assert.Equal(3, total);
        Assert.All(items, p => Assert.True(p.IsActive));
    }

    [Fact]
    public async Task GetPagedAsync_SearchFiltersByName()
    {
        var (items, total) = await _service.GetPagedAsync(search: "iPhone");

        Assert.Equal(1, total);
        Assert.Single(items);
        Assert.Equal("iPhone 15", items.First().Name);
    }

    [Fact]
    public async Task GetPagedAsync_CategoryFilter_ReturnsOnlyThatCategory()
    {
        var (items, total) = await _service.GetPagedAsync(categoryId: 2);

        Assert.Equal(1, total);
        Assert.All(items, p => Assert.Equal(2, p.CategoryId));
    }

    [Fact]
    public async Task GetPagedAsync_PriceRange_FiltersCorrectly()
    {
        var (items, total) = await _service.GetPagedAsync(minPrice: 750m, maxPrice: 900m);

        Assert.Equal(1, total);
        Assert.Single(items);
        Assert.Equal("iPhone 15", items.First().Name);
    }

    [Fact]
    public async Task GetPagedAsync_Paging_LimitsResults()
    {
        var (items, total) = await _service.GetPagedAsync(page: 1, pageSize: 2);

        Assert.Equal(3, total);
        Assert.Equal(2, items.Count());
    }

    [Fact]
    public async Task GetByIdAsync_ExistingProduct_ReturnsDto()
    {
        var product = await _service.GetByIdAsync(1);

        Assert.NotNull(product);
        Assert.Equal("Dell XPS", product.Name);
        Assert.Equal(1000m, product.Price);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingProduct_ReturnsNull()
    {
        var product = await _service.GetByIdAsync(999);
        Assert.Null(product);
    }

    [Fact]
    public async Task GetByIdAsync_InactiveProduct_ReturnsNull()
    {
        var product = await _service.GetByIdAsync(4);
        Assert.Null(product);
    }

    [Fact]
    public async Task CreateProductAsync_ValidRequest_CreatesProduct()
    {
        var request = new ProductCreateRequest("New Product", "Description", 500m, 10, 1, null);

        var (success, message, productId) = await _service.CreateProductAsync(null, request);

        Assert.True(success);
        Assert.Equal("تم إنشاء المنتج بنجاح.", message);
        Assert.NotNull(productId);
    }

    [Fact]
    public async Task CreateProductAsync_EmptyName_ReturnsError()
    {
        var request = new ProductCreateRequest("", "Description", 500m, 10, 1, null);

        var (success, message, _) = await _service.CreateProductAsync(null, request);

        Assert.False(success);
        Assert.Equal("اسم المنتج مطلوب.", message);
    }

    [Fact]
    public async Task CreateProductAsync_InvalidCategory_ReturnsError()
    {
        var request = new ProductCreateRequest("Product", "Description", 500m, 10, 999, null);

        var (success, message, _) = await _service.CreateProductAsync(null, request);

        Assert.False(success);
        Assert.Equal("الفئة غير موجودة.", message);
    }

    [Fact]
    public async Task UpdateProductAsync_ValidRequest_UpdatesProduct()
    {
        var request = new ProductUpdateRequest("Updated Name", "New Desc", 900m, 8, 1, true, null);

        var (success, message) = await _service.UpdateProductAsync(1, request);

        Assert.True(success);
        Assert.Equal("تم تحديث المنتج بنجاح.", message);
    }

    [Fact]
    public async Task UpdateProductAsync_ProductNotFound_ReturnsError()
    {
        var request = new ProductUpdateRequest("Name", "Desc", 100m, 5, 1, true, null);

        var (success, message) = await _service.UpdateProductAsync(999, request);

        Assert.False(success);
        Assert.Equal("المنتج غير موجود.", message);
    }

    [Fact]
    public async Task DeactivateProductAsync_ValidId_Deactivates()
    {
        var (success, message) = await _service.DeactivateProductAsync(1);

        Assert.True(success);
        Assert.Equal("تم تعطيل المنتج بنجاح.", message);
    }

    [Fact]
    public async Task DeactivateProductAsync_ProductNotFound_ReturnsError()
    {
        var (success, message) = await _service.DeactivateProductAsync(999);

        Assert.False(success);
        Assert.Equal("المنتج غير موجود.", message);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
