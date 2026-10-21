using System.Linq.Expressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TechXpress.Data.Entities;
using TechXpress.Data.Repositories.Interfaces;
using TechXpress.Data.UoW;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Implementations;
using TechXpress.Services.Models;

namespace TechXpress.Services.Tests;

public class WalletServiceTests : IDisposable
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ILogger<WalletService>> _loggerMock;
    private readonly WalletService _service;

    private readonly Vendor _vendor;
    private readonly Order _order;
    private readonly OrderDetail _orderDetail;
    private readonly WalletTransaction _deposit;

    public WalletServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<WalletService>>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"Platform:CommissionRate", "0.10"}
            })
            .Build();

        _service = new WalletService(_uowMock.Object, _loggerMock.Object, config);

        _vendor = new Vendor
        {
            Id = 1, UserId = "vendor-user", StoreName = "Test Store",
            NationalIdPath = "/files/nid.pdf", CommercialRegisterPath = "/files/cr.pdf", TaxCardPath = "/files/tc.pdf",
            Status = VendorStatus.Approved, WalletBalance = 1000m
        };

        _order = new Order
        {
            Id = 100, UserId = "user-1", Status = OrderStatus.Paid, TotalAmount = 200m
        };

        _orderDetail = new OrderDetail
        {
            Id = 10, OrderId = 100, ProductId = 1, Quantity = 2, UnitPrice = 100m, VendorId = 1,
            Product = new Product { Name = "Product", VendorId = 1 }
        };

        _deposit = new WalletTransaction
        {
            Id = 1, VendorId = 1, OrderDetailId = 10, Amount = 180m, BalanceAfter = 1000m,
            Type = WalletTransactionType.Deposit, Status = WalletTransactionStatus.Completed
        };
    }

    private void SetupOrderRepository(Order? order = null)
    {
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(order);
        orderRepo.Setup(x => x.GetByIdAsync(100)).ReturnsAsync(order);
        _uowMock.Setup(x => x.Orders).Returns(orderRepo.Object);
    }

    private void SetupOrderDetailsRepository(List<OrderDetail> details)
    {
        var odRepo = new Mock<IGenericRepository<OrderDetail>>();
        odRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<OrderDetail, bool>>>()))
            .ReturnsAsync(details);
        _uowMock.Setup(x => x.OrderDetails).Returns(odRepo.Object);
    }

    private void SetupWalletsRepository(List<WalletTransaction> transactions, Vendor? vendor = null)
    {
        var walletRepo = new Mock<IWalletRepository>();
        walletRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<WalletTransaction, bool>>>()))
            .ReturnsAsync(transactions);
        walletRepo.Setup(x => x.AddAsync(It.IsAny<WalletTransaction>()))
            .Returns(Task.CompletedTask);
        walletRepo.Setup(x => x.GetByIdWithVendorAsync(1))
            .ReturnsAsync(new WalletTransaction
            {
                Id = 1, VendorId = 1, Amount = 200m, BalanceAfter = 800m,
                Type = WalletTransactionType.Withdrawal, Status = WalletTransactionStatus.Pending,
                Vendor = vendor ?? _vendor
            });
        _uowMock.Setup(x => x.Wallets).Returns(walletRepo.Object);
    }

    private void SetupVendorsRepository(Vendor? vendor = null)
    {
        var vendorRepo = new Mock<IVendorRepository>();
        vendorRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(vendor ?? _vendor);
        _uowMock.Setup(x => x.Vendors).Returns(vendorRepo.Object);
    }

    private void SetupUnitOfWorkForTransaction()
    {
        _uowMock.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync()).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
    }

    [Fact]
    public async Task ProcessOrderCommissionAsync_OrderNotPaid_DoesNothing()
    {
        SetupOrderRepository(new Order { Id = 100, Status = OrderStatus.Pending });

        await _service.ProcessOrderCommissionAsync(100);

        // Should not throw, just log warning and return
        Assert.True(true);
    }

    [Fact]
    public async Task ProcessOrderCommissionAsync_OrderNotFound_DoesNothing()
    {
        SetupOrderRepository(null);
        await _service.ProcessOrderCommissionAsync(999);
        Assert.True(true);
    }

    [Fact]
    public async Task ProcessOrderCommissionAsync_PaidOrder_DepositsNetEarnings()
    {
        SetupOrderRepository(_order);
        SetupOrderDetailsRepository(new List<OrderDetail> { _orderDetail });
        SetupVendorsRepository(_vendor);
        SetupUnitOfWorkForTransaction();

        // HasDepositForOrderDetail returns false (not yet processed)
        var walletRepo = new Mock<IWalletRepository>();
        walletRepo.Setup(x => x.HasDepositForOrderDetailAsync(10)).ReturnsAsync(false);
        _uowMock.Setup(x => x.Wallets).Returns(walletRepo.Object);

        await _service.ProcessOrderCommissionAsync(100);

        // Gross = 100 * 2 = 200, Commission = 20, Net = 180
        // Vendor balance should be 1000 + 180 = 1180
        Assert.Equal(1180m, _vendor.WalletBalance);
    }

    [Fact]
    public async Task ReverseOrderCommissionAsync_OrderNotPaid_DoesNothing()
    {
        SetupOrderRepository(new Order { Id = 100, Status = OrderStatus.Pending });
        await _service.ReverseOrderCommissionAsync(100);
        Assert.True(true);
    }

    [Fact]
    public async Task ReverseOrderCommissionAsync_PaidOrderWithDeposit_Reverses()
    {
        SetupOrderRepository(_order);
        SetupOrderDetailsRepository(new List<OrderDetail> { _orderDetail });
        SetupVendorsRepository(_vendor);
        SetupUnitOfWorkForTransaction();

        var walletRepo = new Mock<IWalletRepository>();
        walletRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<WalletTransaction, bool>>>()))
            .ReturnsAsync(new List<WalletTransaction> { _deposit });
        _uowMock.Setup(x => x.Wallets).Returns(walletRepo.Object);

        await _service.ReverseOrderCommissionAsync(100);

        // Vendor balance: 1000 - 180 = 820
        Assert.Equal(820m, _vendor.WalletBalance);
    }

    [Fact]
    public async Task RequestPayoutAsync_ValidRequest_SuccessAndReservesAmount()
    {
        SetupVendorsRepository(_vendor);
        SetupWalletsRepository(new List<WalletTransaction>());
        SetupUnitOfWorkForTransaction();

        var result = await _service.RequestPayoutAsync(1, 500m, "فودافون كاش: 01000000000");

        Assert.True(result.Success);
        // Amount reserved: 1000 - 500 = 500
        Assert.Equal(500m, _vendor.WalletBalance);
    }

    [Fact]
    public async Task RequestPayoutAsync_ZeroAmount_ReturnsError()
    {
        var result = await _service.RequestPayoutAsync(1, 0m, "details");
        Assert.False(result.Success);
        Assert.Contains("أكبر من الصفر", result.Message);
    }

    [Fact]
    public async Task RequestPayoutAsync_InsufficientBalance_ReturnsError()
    {
        SetupVendorsRepository(_vendor);
        var result = await _service.RequestPayoutAsync(1, 2000m, "details");
        Assert.False(result.Success);
        Assert.Contains("غير كافٍ", result.Message);
    }

    [Fact]
    public async Task RequestPayoutAsync_NotApprovedVendor_ReturnsError()
    {
        var notApproved = new Vendor { Id = 1, Status = VendorStatus.Pending, WalletBalance = 1000m };
        SetupVendorsRepository(notApproved);
        var result = await _service.RequestPayoutAsync(1, 100m, "details");
        Assert.False(result.Success);
        Assert.Contains("غير معتمد", result.Message);
    }

    [Fact]
    public async Task ProcessPayoutAsync_Approve_MarksCompleted()
    {
        SetupWalletsRepository(new List<WalletTransaction>(), _vendor);
        SetupUnitOfWorkForTransaction();

        var result = await _service.ProcessPayoutAsync(1, true, "admin-1");

        Assert.True(result.Success);
        Assert.Contains("تمت الموافقة", result.Message);
    }

    [Fact]
    public async Task ProcessPayoutAsync_Reject_ReturnsAmountToWallet()
    {
        var vendorForReject = new Vendor { Id = 1, WalletBalance = 800m };
        SetupWalletsRepository(new List<WalletTransaction>(), vendorForReject);
        SetupUnitOfWorkForTransaction();

        var result = await _service.ProcessPayoutAsync(1, false, "admin-1", "سبب الرفض");

        Assert.True(result.Success);
        // Amount returned: 800 + 200 = 1000
        Assert.Equal(1000m, vendorForReject.WalletBalance);
    }

    [Fact]
    public async Task ProcessPayoutAsync_NotFoundOrNotPending_ReturnsError()
    {
        var walletRepo = new Mock<IWalletRepository>();
        walletRepo.Setup(x => x.GetByIdWithVendorAsync(999)).ReturnsAsync((WalletTransaction?)null);
        _uowMock.Setup(x => x.Wallets).Returns(walletRepo.Object);

        var result = await _service.ProcessPayoutAsync(999, true, "admin-1");
        Assert.False(result.Success);
        Assert.Contains("غير موجود", result.Message);
    }

    // ===================== سجل طلبات السحب والتدقيق =====================

    /// <summary>يجهّز مستودع المحفظة ليُرجع طلب سحب محدد ويجمع أي معاملة جديدة تُضاف.</summary>
    private Mock<IWalletRepository> SetupWalletsForTransaction(
        WalletTransaction tx, List<WalletTransaction>? added = null)
    {
        var walletRepo = new Mock<IWalletRepository>();
        walletRepo.Setup(x => x.GetByIdWithVendorAsync(tx.Id)).ReturnsAsync(tx);
        walletRepo.Setup(x => x.AddAsync(It.IsAny<WalletTransaction>()))
            .Callback<WalletTransaction>(t => added?.Add(t))
            .Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.Wallets).Returns(walletRepo.Object);
        return walletRepo;
    }

    private static WalletTransaction CreatePendingPayout(int id = 7, decimal amount = 200m, decimal balance = 800m)
        => new()
        {
            Id = id, VendorId = 1, Amount = amount, BalanceAfter = balance,
            Type = WalletTransactionType.Withdrawal, Status = WalletTransactionStatus.Pending,
            Description = "طلب سحب أرباح", PayoutDetails = "فودافون كاش: 01000000000",
            Vendor = new Vendor { Id = 1, StoreName = "Demo Store", WalletBalance = balance }
        };

    [Fact]
    public async Task ProcessPayoutAsync_Approve_WritesAuditTrail()
    {
        var tx = CreatePendingPayout();
        SetupWalletsForTransaction(tx);
        SetupUnitOfWorkForTransaction();

        var result = await _service.ProcessPayoutAsync(7, true, "admin-9", null, "مدير النظام");

        Assert.True(result.Success);
        Assert.Equal(WalletTransactionStatus.Completed, tx.Status);
        Assert.Equal("admin-9", tx.ProcessedByUserId);
        Assert.Equal("مدير النظام", tx.ProcessedByName);
        Assert.NotNull(tx.ProcessedAtUtc);
        Assert.Null(tx.RejectionReason);
        Assert.Contains("تم اعتماد الطلب", tx.Description);
    }

    [Fact]
    public async Task ProcessPayoutAsync_Reject_WritesReasonAndCreatesRefund()
    {
        var tx = CreatePendingPayout(8, 200m, 800m);
        var added = new List<WalletTransaction>();
        SetupWalletsForTransaction(tx, added);
        SetupUnitOfWorkForTransaction();

        var result = await _service.ProcessPayoutAsync(8, false, "admin-9", "  بيانات الحساب غير صحيحة  ", "مدير النظام");

        Assert.True(result.Success);
        Assert.Equal(WalletTransactionStatus.Rejected, tx.Status);
        Assert.Equal("بيانات الحساب غير صحيحة", tx.RejectionReason);      // تم تقليم المسافات
        Assert.Equal("admin-9", tx.ProcessedByUserId);
        Assert.NotNull(tx.ProcessedAtUtc);
        Assert.Equal(1000m, tx.Vendor.WalletBalance);                     // 800 + 200 رجعوا للمحفظة

        var refund = Assert.Single(added);
        Assert.Equal(WalletTransactionType.Refund, refund.Type);
        Assert.Equal(WalletTransactionStatus.Completed, refund.Status);
        Assert.Equal(200m, refund.Amount);
        Assert.Equal(1000m, refund.BalanceAfter);
    }

    [Fact]
    public async Task ProcessPayoutAsync_Reject_BlankReason_UsesDefault()
    {
        var tx = CreatePendingPayout(9);
        SetupWalletsForTransaction(tx);
        SetupUnitOfWorkForTransaction();

        await _service.ProcessPayoutAsync(9, false, "admin-9", "   ");

        Assert.Equal("بدون سبب محدد", tx.RejectionReason);
    }

    [Fact]
    public async Task ProcessPayoutAsync_AlreadyProcessed_ReturnsError()
    {
        var tx = CreatePendingPayout(10);
        tx.Status = WalletTransactionStatus.Completed;   // اتعالجت قبل كده
        SetupWalletsForTransaction(tx);

        var result = await _service.ProcessPayoutAsync(10, true, "admin-9");

        Assert.False(result.Success);
        Assert.Contains("تم معالجته سابقاً", result.Message);
    }

    [Fact]
    public async Task GetPayoutRequestCountsAsync_MapsRepositoryCounts()
    {
        var walletRepo = new Mock<IWalletRepository>();
        walletRepo.Setup(x => x.GetPayoutRequestCountsAsync()).ReturnsAsync((10, 3, 5, 2));
        _uowMock.Setup(x => x.Wallets).Returns(walletRepo.Object);

        var counts = await _service.GetPayoutRequestCountsAsync();

        Assert.Equal(10, counts.Total);
        Assert.Equal(3, counts.Pending);
        Assert.Equal(5, counts.Approved);
        Assert.Equal(2, counts.Rejected);
    }

    [Fact]
    public async Task GetPayoutRequestsAsync_MapsVendorNameAndAuditFields()
    {
        var requestedAt = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var processedAt = new DateTime(2026, 1, 2, 9, 30, 0, DateTimeKind.Utc);

        var tx = new WalletTransaction
        {
            Id = 6, VendorId = 3, Amount = 1m, BalanceAfter = 2m,
            Type = WalletTransactionType.Withdrawal, Status = WalletTransactionStatus.Rejected,
            PayoutDetails = "إنستاباي", Description = "سحب أرباح تجريبي", CreatedAt = requestedAt,
            ProcessedAtUtc = processedAt, ProcessedByUserId = "admin-1", ProcessedByName = "مدير النظام",
            RejectionReason = "بيانات الاستلام غير مكتملة",
            Vendor = new Vendor { Id = 3, StoreName = "Demo Store" }
        };

        var walletRepo = new Mock<IWalletRepository>();
        walletRepo.Setup(x => x.GetPayoutRequestsAsync(WalletTransactionStatus.Rejected))
            .ReturnsAsync(new List<WalletTransaction> { tx });
        _uowMock.Setup(x => x.Wallets).Returns(walletRepo.Object);

        var items = await _service.GetPayoutRequestsAsync(WalletTransactionStatus.Rejected);

        var item = Assert.Single(items);
        Assert.Equal(6, item.Id);
        Assert.Equal(3, item.VendorId);
        Assert.Equal("Demo Store", item.StoreName);
        Assert.Equal(1m, item.Amount);
        Assert.Equal("إنستاباي", item.PayoutDetails);
        Assert.Equal(WalletTransactionStatus.Rejected, item.Status);
        Assert.Equal(requestedAt, item.RequestedAt);
        Assert.Equal(processedAt, item.ProcessedAtUtc);
        Assert.Equal("مدير النظام", item.ProcessedByName);
        Assert.Equal("بيانات الاستلام غير مكتملة", item.RejectionReason);
    }

    [Fact]
    public async Task GetPayoutRequestsAsync_MissingVendor_UsesDashPlaceholder()
    {
        var tx = new WalletTransaction
        {
            Id = 11, VendorId = 4, Amount = 5m, Status = WalletTransactionStatus.Pending,
            Type = WalletTransactionType.Withdrawal, Vendor = null!
        };
        var walletRepo = new Mock<IWalletRepository>();
        walletRepo.Setup(x => x.GetPayoutRequestsAsync(null))
            .ReturnsAsync(new List<WalletTransaction> { tx });
        _uowMock.Setup(x => x.Wallets).Returns(walletRepo.Object);

        var items = await _service.GetPayoutRequestsAsync(null);

        Assert.Equal("-", Assert.Single(items).StoreName);
    }

    public void Dispose()
    {
    }
}
