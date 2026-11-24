using System.Text.Json;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Moq;
using TechXpress.Data.Entities;
using TechXpress.Data.Repositories.Interfaces;
using TechXpress.Data.UoW;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Implementations;
using TechXpress.Services.Models;

namespace TechXpress.Services.Tests;

public class OrderServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ILogger<OrderService>> _loggerMock;
    private readonly Mock<ICartService> _cartServiceMock;
    private readonly Mock<IWalletService> _walletServiceMock;
    private readonly OrderService _service;

    private readonly List<CartItemDto> _cartItems;
    private readonly Product _product;
    private int _nextOrderId = 1;

    public OrderServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<OrderService>>();
        _cartServiceMock = new Mock<ICartService>();
        _walletServiceMock = new Mock<IWalletService>();
        _service = new OrderService(_uowMock.Object, _loggerMock.Object, _cartServiceMock.Object, _walletServiceMock.Object);

        _product = new Product
        {
            Id = 1,
            Name = "Test Product",
            Price = 100m,
            StockQuantity = 10,
            CategoryId = 1,
            VendorId = 5,
            IsActive = true
        };

        _cartItems = new List<CartItemDto>
        {
            new CartItemDto(1, "Test Product", 100m, 2, 10, null)
        };

        // Mock cart service: return 2 items
        _cartServiceMock
            .Setup(x => x.GetSummary(It.Is<string>(_ => true), null))
            .Returns(new CartSummaryDto(_cartItems, 200m, 0m, 200m, 2));

        // Mock product repository
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(_product);
        productRepo.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((Product?)null);
        _uowMock.Setup(x => x.Products).Returns(productRepo.Object);

        // Mock orders repository
        var orderRepo = new Mock<IOrderRepository>();
        _uowMock.Setup(x => x.Orders).Returns(orderRepo.Object);

        // Mock order details repository
        var orderDetailsRepo = new Mock<IGenericRepository<OrderDetail>>();
        _uowMock.Setup(x => x.OrderDetails).Returns(orderDetailsRepo.Object);

        // Store credit repository (used for refunds and credit-paid orders)
        var storeCreditRepo = new Mock<IStoreCreditRepository>();
        storeCreditRepo.Setup(x => x.GetBalanceAsync(It.IsAny<string>())).ReturnsAsync(0m);
        storeCreditRepo.Setup(x => x.GetUserAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
        storeCreditRepo.Setup(x => x.HasRefundForOrderAsync(It.IsAny<int>())).ReturnsAsync(false);
        _uowMock.Setup(x => x.StoreCredits).Returns(storeCreditRepo.Object);

        // Capture order Id on AddAsync
        _uowMock.Setup(x => x.Orders.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(o => o.Id = _nextOrderId++)
            .Returns(Task.CompletedTask);

        _uowMock.Setup(x => x.OrderDetails.AddAsync(It.IsAny<OrderDetail>()))
            .Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _uowMock.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync()).Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task PlaceOrderAsync_ValidOrder_ReturnsOrderId()
    {
        var result = await _service.PlaceOrderAsync("user-1", "[{}]", "Cairo, Egypt");

        Assert.True(result.Success);
        Assert.NotNull(result.OrderId);
        Assert.Equal("تم إنشاء طلبك بنجاح.", result.Message);
        // Verify stock was reduced
        Assert.Equal(8, _product.StockQuantity);
    }

    [Fact]
    public async Task PlaceOrderAsync_EmptyCart_ReturnsError()
    {
        _cartServiceMock
            .Setup(x => x.GetSummary(It.Is<string>(_ => true), null))
            .Returns(new CartSummaryDto(Enumerable.Empty<CartItemDto>(), 0m, 0m, 0m, 0));

        var result = await _service.PlaceOrderAsync("user-1", "[]", "Cairo, Egypt");

        Assert.False(result.Success);
        Assert.Equal("سلة الشراء فارغة.", result.Message);
    }

    [Fact]
    public async Task PlaceOrderAsync_InvalidPromo_ReturnsError()
    {
        _cartServiceMock
            .Setup(x => x.GetSummary(It.Is<string>(_ => true), It.Is<string>(_ => true)))
            .Returns(new CartSummaryDto(_cartItems, 200m, 0m, 200m, 2));

        var result = await _service.PlaceOrderAsync("user-1", "[{}]", "Cairo, Egypt", "INVALID");

        Assert.False(result.Success);
        Assert.Equal("كود الخصم غير صالح.", result.Message);
    }

    [Fact]
    public async Task PlaceOrderAsync_InsufficientStock_ReturnsErrorAndRollsBack()
    {
        _product.StockQuantity = 1; // less than requested 2

        var result = await _service.PlaceOrderAsync("user-1", "[{}]", "Cairo, Egypt");

        Assert.False(result.Success);
        Assert.Contains("غير كافية", result.Message);
        // Stock should not be reduced (rollback)
        Assert.Equal(1, _product.StockQuantity);
    }

    [Fact]
    public async Task PlaceOrderAsync_ProductNotFound_ReturnsError()
    {
        var items = new List<CartItemDto> { new CartItemDto(999, "Missing", 100m, 1, 0, null) };
        _cartServiceMock
            .Setup(x => x.GetSummary(It.Is<string>(_ => true), null))
            .Returns(new CartSummaryDto(items, 100m, 0m, 100m, 1));

        var result = await _service.PlaceOrderAsync("user-1", "[{}]", "Cairo, Egypt");

        Assert.False(result.Success);
        Assert.Contains("لم يعد موجودًا", result.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_PendingToPaid_Allowed()
    {
        var order = new Order { Id = 1, Status = OrderStatus.Pending, UserId = "user-1", TotalAmount = 100m };
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(order);
        _uowMock.Setup(x => x.Orders).Returns(orderRepo.Object);

        var result = await _service.UpdateStatusAsync(1, OrderStatus.Paid);

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_PendingToShipped_NotAllowed()
    {
        var order = new Order { Id = 1, Status = OrderStatus.Pending, UserId = "user-1", TotalAmount = 100m };
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(order);
        _uowMock.Setup(x => x.Orders).Returns(orderRepo.Object);

        var result = await _service.UpdateStatusAsync(1, OrderStatus.Shipped);

        Assert.False(result.Success);
        Assert.Contains("غير صحيحة", result.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_OrderNotFound_ReturnsError()
    {
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(x => x.GetByIdWithDetailsAsync(999)).ReturnsAsync((Order?)null);
        _uowMock.Setup(x => x.Orders).Returns(orderRepo.Object);

        var result = await _service.UpdateStatusAsync(999, OrderStatus.Paid);

        Assert.False(result.Success);
        Assert.Equal("الطلب غير موجود.", result.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_PaidToShipped_MarksItemsShipped()
    {
        var order = new Order
        {
            Id = 1, Status = OrderStatus.Paid, UserId = "user-1", TotalAmount = 100m,
            OrderDetails = new List<OrderDetail>
            {
                new() { Id = 1, OrderId = 1, ProductId = 1, Quantity = 1, UnitPrice = 100m, VendorId = 5 }
            }
        };
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(order);
        _uowMock.Setup(x => x.Orders).Returns(orderRepo.Object);

        var result = await _service.UpdateStatusAsync(1, OrderStatus.Shipped);

        Assert.True(result.Success);
        var item = Assert.Single(order.OrderDetails);
        Assert.Equal(ShippingStatus.Shipped, item.ShippingStatus);
        Assert.NotNull(item.ShippedAt);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShippedToDelivered_MarksItemsDelivered()
    {
        var order = new Order
        {
            Id = 1, Status = OrderStatus.Shipped, UserId = "user-1", TotalAmount = 100m,
            OrderDetails = new List<OrderDetail>
            {
                new()
                {
                    Id = 1, OrderId = 1, ProductId = 1, Quantity = 1, UnitPrice = 100m,
                    VendorId = 5, ShippingStatus = ShippingStatus.Shipped
                }
            }
        };
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(order);
        _uowMock.Setup(x => x.Orders).Returns(orderRepo.Object);

        var result = await _service.UpdateStatusAsync(1, OrderStatus.Delivered);

        Assert.True(result.Success);
        var item = Assert.Single(order.OrderDetails);
        Assert.Equal(ShippingStatus.Delivered, item.ShippingStatus);
        Assert.NotNull(item.DeliveredAt);
    }

    [Fact]
    public async Task CancelOrderAsync_Valid_RestoresStockAndCancels()
    {
        var order = new Order
        {
            Id = 1, UserId = "user-1", Status = OrderStatus.Pending, TotalAmount = 100m
        };
        var orderDetail = new OrderDetail
        {
            Id = 1, OrderId = 1, ProductId = 1, Quantity = 2, UnitPrice = 100m, VendorId = 5,
            Product = _product
        };
        order.OrderDetails.Add(orderDetail);

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(x => x.GetByIdForUserAsync(1, "user-1")).ReturnsAsync(order);

        var details = new List<OrderDetail> { orderDetail };
        var orderDetailsRepo = new Mock<IGenericRepository<OrderDetail>>();
        orderDetailsRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<OrderDetail, bool>>>()))
            .ReturnsAsync(details);

        _uowMock.Setup(x => x.Orders).Returns(orderRepo.Object);
        _uowMock.Setup(x => x.OrderDetails).Returns(orderDetailsRepo.Object);

        var result = await _service.CancelOrderAsync(1, "user-1");

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        // Stock restored: 10 + 2 = 12
        Assert.Equal(12, _product.StockQuantity);
    }

    [Fact]
    public async Task CancelOrderAsync_AlreadyCancelled_ReturnsError()
    {
        var order = new Order { Id = 1, UserId = "user-1", Status = OrderStatus.Cancelled, TotalAmount = 100m };
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(x => x.GetByIdForUserAsync(1, "user-1")).ReturnsAsync(order);
        _uowMock.Setup(x => x.Orders).Returns(orderRepo.Object);

        var result = await _service.CancelOrderAsync(1, "user-1");

        Assert.False(result.Success);
        Assert.Equal("تم إلغاء هذا الطلب بالفعل.", result.Message);
    }

    [Fact]
    public async Task CancelOrderAsync_Delivered_ReturnsError()
    {
        var order = new Order { Id = 1, UserId = "user-1", Status = OrderStatus.Delivered, TotalAmount = 100m };
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(x => x.GetByIdForUserAsync(1, "user-1")).ReturnsAsync(order);
        _uowMock.Setup(x => x.Orders).Returns(orderRepo.Object);

        var result = await _service.CancelOrderAsync(1, "user-1");

        Assert.False(result.Success);
        Assert.Equal("لا يمكن إلغاء طلب تم توصيله. استخدم خيار إرجاع الطلب.", result.Message);
    }
}
