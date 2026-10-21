using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TechXpress.Data.Context;
using TechXpress.Data.Entities;
using TechXpress.Data.Repositories.Implementations;
using TechXpress.Data.Repositories.Interfaces;
using TechXpress.Services.Implementations;

namespace TechXpress.Services.Tests;

public class UserManagementServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly IUserManagementRepository _repository;
    private readonly UserManagementService _service;

    public UserManagementServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _repository = new UserManagementRepository(_context);
        _service = new UserManagementService(_repository, Mock.Of<ILogger<UserManagementService>>());
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private async Task SeedRolesAsync()
    {
        _context.Roles.Add(new IdentityRole { Id = "r-admin", Name = "Admin", NormalizedName = "ADMIN" });
        _context.Roles.Add(new IdentityRole { Id = "r-vendor", Name = "Vendor", NormalizedName = "VENDOR" });
        _context.Roles.Add(new IdentityRole { Id = "r-customer", Name = "Customer", NormalizedName = "CUSTOMER" });
        await _context.SaveChangesAsync();
    }

    private async Task<ApplicationUser> AddUserAsync(string id, string roleId, bool isActive = true, string? city = null)
    {
        var user = new ApplicationUser
        {
            Id = id,
            UserName = $"{id}@test.com",
            Email = $"{id}@test.com",
            FullName = $"User {id}",
            City = city,
            IsActive = isActive
        };
        _context.Users.Add(user);
        _context.UserRoles.Add(new IdentityUserRole<string> { UserId = id, RoleId = roleId });
        await _context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsUsersWithRoles()
    {
        await SeedRolesAsync();
        await AddUserAsync("admin-1", "r-admin", city: "Cairo");
        await AddUserAsync("cust-1", "r-customer");

        var users = await _service.GetAllUsersAsync();

        Assert.Equal(2, users.Count);
        var admin = users.Single(u => u.UserId == "admin-1");
        Assert.Contains("Admin", admin.Roles);
        Assert.Equal("Cairo", admin.City);
    }

    [Fact]
    public async Task ActivateUserAsync_ExistingUser_Activates()
    {
        await SeedRolesAsync();
        await AddUserAsync("cust-1", "r-customer", isActive: false);

        var (success, _) = await _service.ActivateUserAsync("cust-1");

        Assert.True(success);
        Assert.True((await _context.Users.FindAsync("cust-1"))!.IsActive);
    }

    [Fact]
    public async Task ActivateUserAsync_UnknownUser_ReturnsFalse()
    {
        var (success, _) = await _service.ActivateUserAsync("missing");

        Assert.False(success);
    }

    [Fact]
    public async Task SuspendUserAsync_Self_IsRejected()
    {
        await SeedRolesAsync();
        await AddUserAsync("admin-1", "r-admin");

        var (success, message) = await _service.SuspendUserAsync("admin-1", "admin-1");

        Assert.False(success);
        Assert.Contains("حسابك", message);
    }

    [Fact]
    public async Task SuspendUserAsync_LastActiveAdmin_IsRejected()
    {
        await SeedRolesAsync();
        await AddUserAsync("admin-1", "r-admin"); // الأدمن النشط الوحيد

        var (success, message) = await _service.SuspendUserAsync("admin-1", "different-admin");

        Assert.False(success);
        Assert.Contains("آخر مدير نشط", message);
        Assert.True((await _context.Users.FindAsync("admin-1"))!.IsActive);
    }

    [Fact]
    public async Task SuspendUserAsync_RegularUser_Succeeds()
    {
        await SeedRolesAsync();
        await AddUserAsync("admin-1", "r-admin");
        await AddUserAsync("cust-1", "r-customer");

        var (success, _) = await _service.SuspendUserAsync("cust-1", "admin-1");

        Assert.True(success);
        Assert.False((await _context.Users.FindAsync("cust-1"))!.IsActive);
    }

    [Fact]
    public async Task SuspendUserAsync_AdminWithAnotherActiveAdmin_Succeeds()
    {
        await SeedRolesAsync();
        await AddUserAsync("admin-1", "r-admin");
        await AddUserAsync("admin-2", "r-admin");

        var (success, _) = await _service.SuspendUserAsync("admin-2", "admin-1");

        Assert.True(success);
        Assert.False((await _context.Users.FindAsync("admin-2"))!.IsActive);
    }
}
