using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TechXpress.Data.Entities;
using TechXpress.Data.Repositories.Interfaces;
using TechXpress.Data.UoW;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Implementations;
using TechXpress.Services.Models;

namespace TechXpress.Services.Tests;

public class VendorServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IVendorDocumentStorage> _storageMock;
    private readonly Mock<ILogger<VendorService>> _loggerMock;
    private readonly VendorService _service;

    public VendorServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _storageMock = new Mock<IVendorDocumentStorage>();
        _loggerMock = new Mock<ILogger<VendorService>>();

        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<ApplicationUser>>(),
            Enumerable.Empty<IUserValidator<ApplicationUser>>(),
            Enumerable.Empty<IPasswordValidator<ApplicationUser>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            null,
            Mock.Of<ILogger<UserManager<ApplicationUser>>>()
        );

        _service = new VendorService(_uowMock.Object, _userManagerMock.Object, _storageMock.Object, _loggerMock.Object);

        _uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _uowMock.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync()).Returns(Task.CompletedTask);
    }

    private Vendor CreateVendor(VendorStatus status, string userId = "vendor-user", string? rejectionReason = null)
    {
        return new Vendor
        {
            Id = 1,
            UserId = userId,
            User = new ApplicationUser { Id = userId, FullName = "Test Vendor", Email = "vendor@example.com", UserName = "vendor@example.com" },
            StoreName = "Test Store",
            NationalIdPath = "/files/nid.pdf",
            CommercialRegisterPath = "/files/cr.pdf",
            TaxCardPath = "/files/tc.pdf",
            Status = status,
            RejectionReason = rejectionReason,
            WalletBalance = 0m
        };
    }

    [Fact]
    public async Task GetApplicationStatusAsync_NoVendor_ReturnsNull()
    {
        _uowMock.Setup(x => x.Vendors.GetByUserIdAsync("user-1")).ReturnsAsync((Vendor?)null);

        var result = await _service.GetApplicationStatusAsync("user-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetApplicationStatusAsync_ExistingVendor_ReturnsStatus()
    {
        var vendor = CreateVendor(VendorStatus.Pending);
        _uowMock.Setup(x => x.Vendors.GetByUserIdAsync("user-1")).ReturnsAsync(vendor);

        var result = await _service.GetApplicationStatusAsync("user-1");

        Assert.NotNull(result);
        Assert.Equal(VendorStatus.Pending, result.Status);
        Assert.Equal("Test Store", result.StoreName);
    }

    [Fact]
    public async Task ApplyAsync_ValidApplication_Success()
    {
        var vendor = CreateVendor(VendorStatus.Rejected); // can re-apply after rejection
        _uowMock.Setup(x => x.Vendors.GetByUserIdAsync("user-1")).ReturnsAsync(vendor);

        var files = new[]
        {
            new UploadedFile(new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }), "id.pdf", 5),
            new UploadedFile(new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF }), "cr.jpg", 5),
            new UploadedFile(new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 }), "tc.png", 5)
        };

        _storageMock.Setup(x => x.SaveAsync(It.IsAny<UploadedFile>()))
            .ReturnsAsync(new DocumentSaveResult(true, Guid.NewGuid().ToString("N") + ".pdf", null));

        var request = new VendorApplicationRequest("My Store", files[0], files[1], files[2]);

        var result = await _service.ApplyAsync("user-1", request);

        Assert.True(result.Success);
        Assert.Contains("قيد المراجعة", result.Message);
    }

    [Fact]
    public async Task ApplyAsync_AlreadyApproved_ReturnsError()
    {
        var vendor = CreateVendor(VendorStatus.Approved);
        _uowMock.Setup(x => x.Vendors.GetByUserIdAsync("user-1")).ReturnsAsync(vendor);

        var files = new[]
        {
            new UploadedFile(new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }), "id.pdf", 5),
            new UploadedFile(new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF }), "cr.jpg", 5),
            new UploadedFile(new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 }), "tc.png", 5)
        };
        _storageMock.Setup(x => x.SaveAsync(It.IsAny<UploadedFile>()))
            .ReturnsAsync(new DocumentSaveResult(true, Guid.NewGuid().ToString("N") + ".pdf", null));

        var request = new VendorApplicationRequest("My Store", files[0], files[1], files[2]);

        var result = await _service.ApplyAsync("user-1", request);

        Assert.False(result.Success);
        Assert.Contains("معتمد بالفعل", result.Message);
    }

    [Fact]
    public async Task ApplyAsync_EmptyStoreName_ReturnsError()
    {
        var files = new[]
        {
            new UploadedFile(new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }), "id.pdf", 5),
            new UploadedFile(new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF }), "cr.jpg", 5),
            new UploadedFile(new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 }), "tc.png", 5)
        };
        var request = new VendorApplicationRequest("", files[0], files[1], files[2]);

        var result = await _service.ApplyAsync("user-1", request);

        Assert.False(result.Success);
        Assert.Contains("اسم المتجر مطلوب", result.Message);
    }

    [Fact]
    public async Task GetRequestsAsync_ReturnsAllVendors()
    {
        var vendors = new List<Vendor> { CreateVendor(VendorStatus.Pending), CreateVendor(VendorStatus.Approved) };
        _uowMock.Setup(x => x.Vendors.GetAllWithUsersAsync()).ReturnsAsync(vendors);

        var result = await _service.GetRequestsAsync(null);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ApproveAsync_PendingVendor_Success()
    {
        var vendor = CreateVendor(VendorStatus.Pending);
        _uowMock.Setup(x => x.Vendors.GetByIdWithUserAsync(1)).ReturnsAsync(vendor);
        _userManagerMock.Setup(x => x.IsInRoleAsync(vendor.User, "Vendor")).ReturnsAsync(false);
        _userManagerMock.Setup(x => x.AddToRoleAsync(vendor.User, "Vendor"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _service.ApproveAsync(1, "admin-1");

        Assert.True(result.Success);
        Assert.Equal(VendorStatus.Approved, vendor.Status);
    }

    [Fact]
    public async Task ApproveAsync_AlreadyApproved_ReturnsError()
    {
        var vendor = CreateVendor(VendorStatus.Approved);
        _uowMock.Setup(x => x.Vendors.GetByIdWithUserAsync(1)).ReturnsAsync(vendor);

        var result = await _service.ApproveAsync(1, "admin-1");

        Assert.False(result.Success);
        Assert.Contains("تمت مراجعة هذا الطلب سابقاً", result.Message);
    }

    [Fact]
    public async Task RejectAsync_ValidReason_Success()
    {
        var vendor = CreateVendor(VendorStatus.Pending);
        _uowMock.Setup(x => x.Vendors.GetByIdWithUserAsync(1)).ReturnsAsync(vendor);

        var result = await _service.RejectAsync(1, "admin-1", "مستندات غير واضحة");

        Assert.True(result.Success);
        Assert.Equal(VendorStatus.Rejected, vendor.Status);
        Assert.Equal("مستندات غير واضحة", vendor.RejectionReason);
    }

    [Fact]
    public async Task RejectAsync_EmptyReason_ReturnsError()
    {
        var result = await _service.RejectAsync(1, "admin-1", "");
        Assert.False(result.Success);
        Assert.Equal("سبب الرفض مطلوب.", result.Message);
    }

    [Fact]
    public async Task OpenDocumentAsync_ValidVendor_ReturnsDocument()
    {
        var vendor = CreateVendor(VendorStatus.Approved);
        _uowMock.Setup(x => x.Vendors.GetByIdAsync(1)).ReturnsAsync(vendor);

        var doc = new StoredDocument(new MemoryStream(), "application/pdf");
        _storageMock.Setup(x => x.Open(vendor.NationalIdPath)).Returns(doc);

        var result = await _service.OpenDocumentAsync(1, VendorDocumentType.NationalId, "admin-1");

        Assert.NotNull(result);
        Assert.Equal("application/pdf", result.ContentType);
        result.Content.Dispose();
    }

    [Fact]
    public async Task GetVendorIdByUserIdAsync_ExistingVendor_ReturnsId()
    {
        var vendor = CreateVendor(VendorStatus.Approved);
        _uowMock.Setup(x => x.Vendors.GetByUserIdAsync("user-1")).ReturnsAsync(vendor);

        var result = await _service.GetVendorIdByUserIdAsync("user-1");

        Assert.Equal(1, result);
    }

    // ===================== إحصائيات لوحة تحكم البائع =====================

    private void SetupDashboardData(
        List<Product> products, List<Order> orders, List<WalletTransaction> transactions)
    {
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(x => x.GetByVendorIdAsync(1)).ReturnsAsync(products);
        _uowMock.Setup(x => x.Products).Returns(productRepo.Object);

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(x => x.GetOrdersByVendorIdAsync(1)).ReturnsAsync(orders);
        _uowMock.Setup(x => x.Orders).Returns(orderRepo.Object);

        var walletRepo = new Mock<IWalletRepository>();
        walletRepo.Setup(x => x.GetTransactionsByVendorIdAsync(1)).ReturnsAsync(transactions);
        _uowMock.Setup(x => x.Wallets).Returns(walletRepo.Object);
    }

    private static Product CreateProductForDashboard(int id, bool isActive = true)
        => new() { Id = id, Name = "Product " + id, Price = 100m, StockQuantity = 5, IsActive = isActive, VendorId = 1 };

    private static Order CreateOrderForDashboard(int id, OrderStatus status, decimal lineTotal)
        => new()
        {
            Id = id,
            UserId = "user-1",
            Status = status,
            OrderDetails = new List<OrderDetail>
            {
                new()
                {
                    Id = id * 100,
                    OrderId = id,
                    ProductId = 1,
                    Quantity = 1,
                    UnitPrice = lineTotal,
                    VendorId = 1,
                    ShippingStatus = status switch
                    {
                        OrderStatus.Shipped => ShippingStatus.Shipped,
                        OrderStatus.Delivered => ShippingStatus.Delivered,
                        _ => ShippingStatus.Pending
                    }
                }
            }
        };

    // ===== الشحن (مسؤولية البائع) =====

    private static Order CreateOrderForShipping(
        int id, OrderStatus status, params (int VendorId, ShippingStatus Shipping)[] items)
    {
        var order = new Order
        {
            Id = id,
            UserId = "user-1",
            OrderDate = DateTime.UtcNow,
            Status = status,
            OrderDetails = new List<OrderDetail>()
        };

        var productId = 1;
        foreach (var (vendorId, shipping) in items)
        {
            order.OrderDetails.Add(new OrderDetail
            {
                Id = id * 100 + productId,
                OrderId = id,
                ProductId = productId++,
                Quantity = 1,
                UnitPrice = 100m,
                VendorId = vendorId,
                ShippingStatus = shipping
            });
        }

        return order;
    }

    private void SetupOrderForShipping(Order order)
    {
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(x => x.GetByIdWithDetailsAsync(order.Id)).ReturnsAsync(order);
        _uowMock.Setup(x => x.Orders).Returns(orderRepo.Object);
    }

    [Fact]
    public async Task GetDashboardStatsAsync_AggregatesProductsOrdersAndSales()
    {
        var products = new List<Product>
        {
            CreateProductForDashboard(1),
            CreateProductForDashboard(2),
            CreateProductForDashboard(3, isActive: false)
        };

        var orders = new List<Order>
        {
            CreateOrderForDashboard(1, OrderStatus.Paid, 200m),
            CreateOrderForDashboard(2, OrderStatus.Shipped, 300m),
            CreateOrderForDashboard(3, OrderStatus.Delivered, 100.5m),
            CreateOrderForDashboard(4, OrderStatus.Cancelled, 999m),
            CreateOrderForDashboard(5, OrderStatus.Pending, 500m)
        };

        var transactions = new List<WalletTransaction>
        {
            new() { Id = 1, VendorId = 1, Amount = 10m, Type = WalletTransactionType.Withdrawal, Status = WalletTransactionStatus.Pending },
            new() { Id = 2, VendorId = 1, Amount = 10m, Type = WalletTransactionType.Withdrawal, Status = WalletTransactionStatus.Completed },
            new() { Id = 3, VendorId = 1, Amount = 10m, Type = WalletTransactionType.Deposit, Status = WalletTransactionStatus.Pending }
        };

        SetupDashboardData(products, orders, transactions);

        var stats = await _service.GetDashboardStatsAsync(1);

        Assert.Equal(3, stats.TotalProducts);
        Assert.Equal(2, stats.ActiveProducts);
        Assert.Equal(1, stats.InactiveProducts);
        Assert.Equal(5, stats.TotalOrders);
        Assert.Equal(1, stats.AwaitingShipment);
        Assert.Equal(1, stats.ShippedOrders);
        Assert.Equal(1, stats.DeliveredOrders);
        Assert.Equal(1, stats.CancelledOrders);
        Assert.Equal(600.5m, stats.TotalSales);   // الطلب غير المدفوع والملغي لا يُحسبان كمبيعات
        Assert.Equal(1, stats.PendingPayouts);    // طلب سحب معلّق واحد فقط
    }

    [Fact]
    public async Task GetDashboardStatsAsync_NoData_ReturnsZeros()
    {
        SetupDashboardData(new List<Product>(), new List<Order>(), new List<WalletTransaction>());

        var stats = await _service.GetDashboardStatsAsync(1);

        Assert.Equal(0, stats.TotalProducts);
        Assert.Equal(0, stats.TotalOrders);
        Assert.Equal(0m, stats.TotalSales);
        Assert.Equal(0, stats.PendingPayouts);
    }

    // ===== الشحن (مسؤولية البائع) =====

    [Fact]
    public async Task ShipOrderAsync_MarksVendorItemsShipped_AndAdvancesOrderToShipped()
    {
        var order = CreateOrderForShipping(10, OrderStatus.Paid, (1, ShippingStatus.Pending));
        SetupOrderForShipping(order);

        var (success, message) = await _service.ShipOrderAsync(1, 10, "Aramex", "TRK-1001");

        Assert.True(success);
        Assert.Contains("تم شحن", message);
        var item = Assert.Single(order.OrderDetails);
        Assert.Equal(ShippingStatus.Shipped, item.ShippingStatus);
        Assert.Equal("Aramex", item.Carrier);
        Assert.Equal("TRK-1001", item.TrackingNumber);
        Assert.NotNull(item.ShippedAt);
        Assert.Equal(OrderStatus.Shipped, order.Status);
        _uowMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ShipOrderAsync_MultiVendor_KeepsOrderPaidUntilEveryVendorShips()
    {
        var order = CreateOrderForShipping(
            11, OrderStatus.Paid, (1, ShippingStatus.Pending), (2, ShippingStatus.Pending));
        SetupOrderForShipping(order);

        var (success, _) = await _service.ShipOrderAsync(1, 11, "DHL", null);

        Assert.True(success);
        Assert.Equal(ShippingStatus.Shipped, order.OrderDetails.First(d => d.VendorId == 1).ShippingStatus);
        Assert.Equal(ShippingStatus.Pending, order.OrderDetails.First(d => d.VendorId == 2).ShippingStatus);
        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Fact]
    public async Task ShipOrderAsync_RejectsOrderThatIsNotPaid()
    {
        var order = CreateOrderForShipping(12, OrderStatus.Pending, (1, ShippingStatus.Pending));
        SetupOrderForShipping(order);

        var (success, message) = await _service.ShipOrderAsync(1, 12, "Aramex", null);

        Assert.False(success);
        Assert.Contains("دفعه", message);
        Assert.Equal(ShippingStatus.Pending, order.OrderDetails.First().ShippingStatus);
        _uowMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ShipOrderAsync_RejectsVendorThatHasNoItemsInOrder()
    {
        var order = CreateOrderForShipping(13, OrderStatus.Paid, (2, ShippingStatus.Pending));
        SetupOrderForShipping(order);

        var (success, message) = await _service.ShipOrderAsync(1, 13, "Aramex", null);

        Assert.False(success);
        Assert.Contains("لا تملك", message);
    }

    [Fact]
    public async Task ShipOrderAsync_RequiresCarrierName()
    {
        var order = CreateOrderForShipping(14, OrderStatus.Paid, (1, ShippingStatus.Pending));
        SetupOrderForShipping(order);

        var (success, message) = await _service.ShipOrderAsync(1, 14, "  ", null);

        Assert.False(success);
        Assert.Contains("شركة الشحن", message);
    }

    [Fact]
    public async Task MarkDeliveredAsync_RequiresShippedItems()
    {
        var order = CreateOrderForShipping(15, OrderStatus.Paid, (1, ShippingStatus.Pending));
        SetupOrderForShipping(order);

        var (success, message) = await _service.MarkDeliveredAsync(1, 15);

        Assert.False(success);
        Assert.Contains("شحن", message);
        Assert.Equal(ShippingStatus.Pending, order.OrderDetails.First().ShippingStatus);
    }

    [Fact]
    public async Task MarkDeliveredAsync_AllItemsDelivered_AdvancesOrderToDelivered()
    {
        var order = CreateOrderForShipping(
            16, OrderStatus.Shipped, (1, ShippingStatus.Shipped), (2, ShippingStatus.Shipped));
        SetupOrderForShipping(order);

        var (success, _) = await _service.MarkDeliveredAsync(1, 16);

        Assert.True(success);
        Assert.Equal(ShippingStatus.Delivered, order.OrderDetails.First(d => d.VendorId == 1).ShippingStatus);
        Assert.NotNull(order.OrderDetails.First(d => d.VendorId == 1).DeliveredAt);
        // البائع الآخر لم يسلّم بعد، لذا يبقى الطلب مشحونًا
        Assert.Equal(OrderStatus.Shipped, order.Status);

        var (secondSuccess, _) = await _service.MarkDeliveredAsync(2, 16);

        Assert.True(secondSuccess);
        Assert.All(order.OrderDetails, d => Assert.Equal(ShippingStatus.Delivered, d.ShippingStatus));
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public async Task StartPreparingAsync_SetsVendorItemsToPreparing()
    {
        var order = CreateOrderForShipping(17, OrderStatus.Paid, (1, ShippingStatus.Pending));
        SetupOrderForShipping(order);

        var (success, message) = await _service.StartPreparingAsync(1, 17);

        Assert.True(success);
        Assert.Contains("تجهيز", message);
        Assert.Equal(ShippingStatus.Preparing, order.OrderDetails.First().ShippingStatus);
        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Fact]
    public async Task GetVendorOrdersAsync_DerivesPerVendorShippingStatus()
    {
        var order = CreateOrderForShipping(
            18, OrderStatus.Paid, (1, ShippingStatus.Shipped));
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(x => x.GetOrdersByVendorIdAsync(1)).ReturnsAsync(new[] { order });
        _uowMock.Setup(x => x.Orders).Returns(orderRepo.Object);

        var result = await _service.GetVendorOrdersAsync(1);

        var summary = Assert.Single(result);
        Assert.Equal(ShippingStatus.Shipped, summary.ShippingStatus);
        Assert.Equal(1, summary.ItemsCount);
    }
}
