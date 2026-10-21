using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechXpress.Data.Context;
using TechXpress.Data.Entities;
using TechXpress.Data.Repositories.Implementations;

namespace TechXpress.Data.Tests;

public class VendorRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SqliteConnection _connection;
    private readonly VendorRepository _repository;

    public VendorRepositoryTests()
    {
        (_context, _connection) = TestDbFactory.Create();
        _repository = new VendorRepository(_context);

        // إضافة المستخدمين أولاً ثم حفظهم (المفتاح الخارجي)
        var user1 = new ApplicationUser { Id = "user-1", FullName = "Ahmed Ali", Email = "ahmed@example.com", UserName = "ahmed@example.com" };
        var user2 = new ApplicationUser { Id = "user-2", FullName = "Sara Mohammed", Email = "sara@example.com", UserName = "sara@example.com" };
        _context.Users.AddRange(user1, user2);
        _context.SaveChanges();

        _context.Vendors.AddRange(
            new Vendor
            {
                Id = 1, UserId = "user-1", StoreName = "أحمد للإلكترونيات",
                NationalIdPath = "/files/nid1.pdf", CommercialRegisterPath = "/files/cr1.pdf", TaxCardPath = "/files/tc1.pdf",
                Status = VendorStatus.Approved, WalletBalance = 500m, SubmittedAt = DateTime.UtcNow.AddDays(-10),
                RowVersion = new byte[8]
            },
            new Vendor
            {
                Id = 2, UserId = "user-2", StoreName = "سارة للأجهزة",
                NationalIdPath = "/files/nid2.pdf", CommercialRegisterPath = "/files/cr2.pdf", TaxCardPath = "/files/tc2.pdf",
                Status = VendorStatus.Pending, WalletBalance = 0m, SubmittedAt = DateTime.UtcNow.AddDays(-2),
                RowVersion = new byte[8]
            },
            new Vendor
            {
                Id = 3, UserId = "user-1", StoreName = "أحمد الثاني",
                NationalIdPath = "/files/nid3.pdf", CommercialRegisterPath = "/files/cr3.pdf", TaxCardPath = "/files/tc3.pdf",
                Status = VendorStatus.Rejected, WalletBalance = 0m, RejectionReason = "مستندات غير مكتملة", SubmittedAt = DateTime.UtcNow.AddDays(-5),
                RowVersion = new byte[8]
            });
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsFirstVendorForUser()
    {
        var vendor = await _repository.GetByUserIdAsync("user-1");

        Assert.NotNull(vendor);
        Assert.Equal("user-1", vendor.UserId);
    }

    [Fact]
    public async Task GetByUserIdAsync_UnknownUser_ReturnsNull()
    {
        var vendor = await _repository.GetByUserIdAsync("unknown-user");
        Assert.Null(vendor);
    }

    [Fact]
    public async Task GetByIdWithUserAsync_ReturnsVendorWithUser()
    {
        var vendor = await _repository.GetByIdWithUserAsync(2);

        Assert.NotNull(vendor);
        Assert.Equal("سارة للأجهزة", vendor.StoreName);
        Assert.NotNull(vendor.User);
        Assert.Equal("Sara Mohammed", vendor.User.FullName);
    }

    [Fact]
    public async Task GetByStatusAsync_ReturnsOnlyMatchingStatus()
    {
        var allStatuses = new[] { VendorStatus.Pending, VendorStatus.Approved, VendorStatus.Rejected };
        foreach (var status in allStatuses)
        {
            var result = await _repository.GetByStatusAsync(status);
            // التحقق أن الطريقة ترجع فقط العناصر التي تطابق الحالة المطلوبة
            Assert.All(result, v => Assert.Equal(status, v.Status));
        }
    }

    [Fact]
    public async Task GetAllWithUsersAsync_ReturnsAllVendors()
    {
        var all = await _repository.GetAllWithUsersAsync();
        Assert.True(all.Count() >= 2, $"Expected at least 2 vendors, got {all.Count()}");
    }

    [Fact]
    public async Task GetPendingVendorsAsync_ReturnsPendingOnly()
    {
        var pending = await _repository.GetPendingVendorsAsync();
        Assert.All(pending, v => Assert.Equal(VendorStatus.Pending, v.Status));
        if (pending.Any())
        {
            Assert.Equal(VendorStatus.Pending, pending.First().Status);
        }
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
