using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Implementations;
using TechXpress.Services.Models;

namespace TechXpress.Services.Tests;

public class CartServiceTests : IDisposable
{
    private readonly CartService _service;
    private readonly Mock<ILogger<CartService>> _loggerMock;

    public CartServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"Promotions:0:Code", "SAVE10"},
                {"Promotions:0:Percent", "10"},
                {"Promotions:1:Code", "SAVE20"},
                {"Promotions:1:Percent", "20"}
            })
            .Build();

        _loggerMock = new Mock<ILogger<CartService>>();
        _service = new CartService(_loggerMock.Object, config);
    }

    [Fact]
    public void GetSummary_EmptyCart_ReturnsZeroTotals()
    {
        var result = _service.GetSummary("[]");

        Assert.Equal(0, result.ItemCount);
        Assert.Equal(0m, result.Subtotal);
        Assert.Equal(0m, result.Total);
        Assert.Empty(result.Items);
    }

    [Fact]
    public void GetSummary_WithItems_CalculatesTotalsCorrectly()
    {
        var cartJson = """
            [
              {"productId":1,"name":"Product A","price":100,"quantity":2,"stockQuantity":10,"imageUrl":null},
              {"productId":2,"name":"Product B","price":50,"quantity":1,"stockQuantity":5,"imageUrl":null}
            ]
            """;

        var result = _service.GetSummary(cartJson);

        Assert.Equal(3, result.ItemCount);
        Assert.Equal(250m, result.Subtotal);
        Assert.Equal(0m, result.Discount);
        Assert.Equal(250m, result.Total);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public void GetSummary_WithValidPromo_AppliesDiscount()
    {
        var cartJson = """
            [{"productId":1,"name":"Product A","price":100,"quantity":2,"stockQuantity":10,"imageUrl":null}]
            """;

        var result = _service.GetSummary(cartJson, "SAVE10");

        Assert.Equal(200m, result.Subtotal);
        Assert.Equal(20m, result.Discount);
        Assert.Equal(180m, result.Total);
    }

    [Fact]
    public void GetSummary_WithInvalidPromo_NoDiscount()
    {
        var cartJson = """
            [{"productId":1,"name":"Product A","price":100,"quantity":2,"stockQuantity":10,"imageUrl":null}]
            """;

        var result = _service.GetSummary(cartJson, "INVALID");

        Assert.Equal(0m, result.Discount);
        Assert.Equal(200m, result.Total);
    }

    [Fact]
    public void AddItem_Valid_AddsToCart()
    {
        var products = new List<CartItemDto>
        {
            new(1, "Product A", 100m, 10, 10, null)
        };

        var (json, result) = _service.AddItem("[]", 1, 2, products);

        Assert.True(result.Success);
        Assert.Equal("تمت إضافة المنتج إلى السلة.", result.Message);
        var count = _service.GetItemCount(json);
        Assert.Equal(2, count);
    }

    [Fact]
    public void AddItem_QuantityZeroOrNegative_ReturnsError()
    {
        var products = new List<CartItemDto>
        {
            new(1, "Product A", 100m, 10, 10, null)
        };

        var (json, result) = _service.AddItem("[]", 1, 0, products);

        Assert.False(result.Success);
        Assert.Equal("الكمية يجب أن تكون أكبر من صفر.", result.Message);
    }

    [Fact]
    public void AddItem_NotEnoughStock_ReturnsError()
    {
        var products = new List<CartItemDto>
        {
            new(1, "Product A", 100m, 2, 2, null)
        };

        var (_, result) = _service.AddItem("[]", 1, 5, products);

        Assert.False(result.Success);
        Assert.Contains("المتاحة في المخزون 2", result.Message);
    }

    [Fact]
    public void AddItem_ProductNotFound_ReturnsError()
    {
        var products = new List<CartItemDto>
        {
            new(1, "Product A", 100m, 10, 10, null)
        };

        var (_, result) = _service.AddItem("[]", 99, 1, products);

        Assert.False(result.Success);
        Assert.Equal("المنتج غير موجود.", result.Message);
    }

    [Fact]
    public void AddItem_ExistingItem_IncreasesQuantity()
    {
        var products = new List<CartItemDto>
        {
            new(1, "Product A", 100m, 10, 10, null)
        };

        var (json1, _) = _service.AddItem("[]", 1, 2, products);
        var (json2, result) = _service.AddItem(json1, 1, 3, products);

        Assert.True(result.Success);
        Assert.Equal(5, _service.GetItemCount(json2));
    }

    [Fact]
    public void UpdateQuantity_Valid_UpdatesQuantity()
    {
        var products = new List<CartItemDto>
        {
            new(1, "Product A", 100m, 10, 10, null)
        };

        var (json1, _) = _service.AddItem("[]", 1, 2, products);
        var (json2, result) = _service.UpdateQuantity(json1, 1, 5);

        Assert.True(result.Success);
        Assert.Equal(5, _service.GetItemCount(json2));
    }

    [Fact]
    public void UpdateQuantity_Zero_RemovesItem()
    {
        var products = new List<CartItemDto>
        {
            new(1, "Product A", 100m, 10, 10, null)
        };

        var (json1, _) = _service.AddItem("[]", 1, 2, products);
        var (json2, result) = _service.UpdateQuantity(json1, 1, 0);

        Assert.True(result.Success);
        Assert.Equal(0, _service.GetItemCount(json2));
    }

    [Fact]
    public void UpdateQuantity_NotFound_ReturnsError()
    {
        var (_, result) = _service.UpdateQuantity("[]", 99, 1);

        Assert.False(result.Success);
        Assert.Equal("العنصر غير موجود في السلة.", result.Message);
    }

    [Fact]
    public void RemoveItem_Valid_RemovesItem()
    {
        var products = new List<CartItemDto>
        {
            new(1, "Product A", 100m, 10, 10, null),
            new(2, "Product B", 50m, 5, 5, null)
        };

        var (json1, _) = _service.AddItem("[]", 1, 1, products);
        var (json2, _) = _service.AddItem(json1, 2, 1, products);
        var (json3, result) = _service.RemoveItem(json2, 1);

        Assert.True(result.Success);
        Assert.Equal(1, _service.GetItemCount(json3));
    }

    [Fact]
    public void RemoveItem_NotFound_ReturnsError()
    {
        var (_, result) = _service.RemoveItem("[]", 99);

        Assert.False(result.Success);
        Assert.Equal("العنصر غير موجود في السلة.", result.Message);
    }

    [Fact]
    public void ClearCart_ReturnsEmptyJson()
    {
        var result = _service.ClearCart();
        Assert.Equal("[]", result);
    }

    public void Dispose()
    {
    }
}
