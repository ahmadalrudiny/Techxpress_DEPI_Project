using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechXpress.Data.Context;
using TechXpress.Data.Entities;
using TechXpress.Data.Repositories.Implementations;

namespace TechXpress.Data.Tests;

public class WalletRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SqliteConnection _connection;
    private readonly WalletRepository _repository;

    public WalletRepositoryTests()
    {
        (_context, _connection) = TestDbFactory.Create();
        _repository = new WalletRepository(_context);

        // إضافة المستخدم أولاً (المفتاح الخارجي للبائع)
        _context.Users.Add(new ApplicationUser
        {
            Id = "vendor-user-1", FullName = "Test Vendor",
            Email = "vendor@example.com", UserName = "vendor@example.com"
        });
        _context.SaveChanges();

        var vendor = new Vendor
        {
            Id = 1, UserId = "vendor-user-1", StoreName = "متجر الاختبار",
            NationalIdPath = "/files/nid.pdf", CommercialRegisterPath = "/files/cr.pdf", TaxCardPath = "/files/tc.pdf",
            Status = VendorStatus.Approved, WalletBalance = 1000m,
            RowVersion = new byte[8]
        };
        _context.Vendors.Add(vendor);
        _context.SaveChanges();

        _context.WalletTransactions.AddRange(
            new WalletTransaction
            {
                Id = 1, VendorId = 1, OrderDetailId = 10, Amount = 500m, BalanceAfter = 500m,
                Type = WalletTransactionType.Deposit, Status = WalletTransactionStatus.Completed,
                Description = "أرباح طلب #100", CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new WalletTransaction
            {
                Id = 2, VendorId = 1, OrderDetailId = 11, Amount = 300m, BalanceAfter = 800m,
                Type = WalletTransactionType.Deposit, Status = WalletTransactionStatus.Completed,
                Description = "أرباح طلب #101", CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new WalletTransaction
            {
                Id = 3, VendorId = 1, Amount = 200m, BalanceAfter = 600m,
                Type = WalletTransactionType.Withdrawal, Status = WalletTransactionStatus.Pending,
                PayoutDetails = "فودافون كاش: 01000000000", Description = "طلب سحب أرباح", CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new WalletTransaction
            {
                Id = 4, VendorId = 99, Amount = 100m, BalanceAfter = 100m,
                Type = WalletTransactionType.Deposit, Status = WalletTransactionStatus.Completed,
                Description = "أرباح بائع آخر", CreatedAt = DateTime.UtcNow.AddDays(-2)
            });
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetTransactionsByVendorIdAsync_ReturnsOnlyThatVendorTransactions()
    {
        var transactions = await _repository.GetTransactionsByVendorIdAsync(1);

        Assert.Equal(3, transactions.Count());
        Assert.All(transactions, t => Assert.Equal(1, t.VendorId));
        // newest first
        Assert.Equal(3, transactions.First().Id);
    }

    [Fact]
    public async Task GetTransactionsByVendorIdAsync_UnknownVendor_ReturnsEmpty()
    {
        var transactions = await _repository.GetTransactionsByVendorIdAsync(999);
        Assert.Empty(transactions);
    }

    [Fact]
    public async Task GetPendingPayoutRequestsAsync_ReturnsPendingWithdrawals()
    {
        var requests = await _repository.GetPendingPayoutRequestsAsync();

        Assert.Single(requests);
        Assert.Equal(3, requests.First().Id);
        Assert.Equal(WalletTransactionType.Withdrawal, requests.First().Type);
        Assert.Equal(WalletTransactionStatus.Pending, requests.First().Status);
    }

    [Fact]
    public async Task GetByIdWithVendorAsync_ReturnsTransactionWithVendor()
    {
        var tx = await _repository.GetByIdWithVendorAsync(3);

        Assert.NotNull(tx);
        Assert.Equal(3, tx.Id);
        Assert.NotNull(tx.Vendor);
        Assert.Equal("متجر الاختبار", tx.Vendor.StoreName);
    }

    [Fact]
    public async Task GetByIdWithVendorAsync_UnknownId_ReturnsNull()
    {
        var tx = await _repository.GetByIdWithVendorAsync(999);
        Assert.Null(tx);
    }

    [Fact]
    public async Task HasDepositForOrderDetailAsync_ExistingDeposit_ReturnsTrue()
    {
        var hasDeposit = await _repository.HasDepositForOrderDetailAsync(10);
        Assert.True(hasDeposit);
    }

    [Fact]
    public async Task HasDepositForOrderDetailAsync_NoDeposit_ReturnsFalse()
    {
        var hasDeposit = await _repository.HasDepositForOrderDetailAsync(999);
        Assert.False(hasDeposit);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
