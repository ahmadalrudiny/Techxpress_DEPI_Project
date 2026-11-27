using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TechXpress.Data.Context;
using TechXpress.Data.Entities;
using TechXpress.Data.UoW;
using TechXpress.Services.Implementations;

namespace TechXpress.Services.Tests;

public class AdminAnalyticsServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly AdminAnalyticsService _service;

    private readonly Order _paid2026;
    private readonly Order _delivered2025;
    private readonly Order _paidAlexandria2026;
    private readonly OrderDetail _paidProductA;
    private readonly OrderDetail _paidProductB;
    private readonly OrderDetail _deliveredProductA;
    private readonly OrderDetail _alexProductB;

    public AdminAnalyticsServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        var category = new Category { Name = "Electronics" };
        var productA = new Product { Name = "Product A", Price = 50m, StockQuantity = 10, Category = category };
        var productB = new Product { Name = "Product B", Price = 20m, StockQuantity = 10, Category = category };
        var user = new ApplicationUser
        {
            Id = "analytics-user",
            UserName = "analytics@test.com",
            NormalizedUserName = "ANALYTICS@TEST.COM",
            Email = "analytics@test.com",
            NormalizedEmail = "ANALYTICS@TEST.COM",
            FullName = "Analytics Test",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        _paid2026 = new Order
        {
            User = user,
            OrderDate = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc),
            Status = OrderStatus.Paid,
            TotalAmount = 100m,
            StoreCreditUsed = 5m,
            DiscountAmount = 10m,
            ShippingAddress = "القاهرة، مدينة نصر، شارع عباس العقاد"
        };
        _paidProductA = new OrderDetail { Product = productA, Quantity = 2, UnitPrice = 50m };
        _paidProductB = new OrderDetail { Product = productB, Quantity = 1, UnitPrice = 20m };
        _paid2026.OrderDetails.Add(_paidProductA);
        _paid2026.OrderDetails.Add(_paidProductB);

        _delivered2025 = new Order
        {
            User = user,
            OrderDate = new DateTime(2025, 5, 10, 10, 0, 0, DateTimeKind.Utc),
            Status = OrderStatus.Delivered,
            TotalAmount = 50m,
            ShippingAddress = "الجيزة، الدقي، شارع التحرير"
        };
        _deliveredProductA = new OrderDetail { Product = productA, Quantity = 1, UnitPrice = 50m };
        _delivered2025.OrderDetails.Add(_deliveredProductA);

        _paidAlexandria2026 = new Order
        {
            User = user,
            OrderDate = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc),
            Status = OrderStatus.Shipped,
            TotalAmount = 20m,
            ShippingAddress = "الإسكندرية، سموحة"
        };
        _alexProductB = new OrderDetail { Product = productB, Quantity = 1, UnitPrice = 20m };
        _paidAlexandria2026.OrderDetails.Add(_alexProductB);

        var cancelled = new Order
        {
            User = user,
            OrderDate = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc),
            Status = OrderStatus.Cancelled,
            TotalAmount = 40m,
            ShippingAddress = "القاهرة"
        };
        var cancelledDetail = new OrderDetail { Product = productB, Quantity = 1, UnitPrice = 40m };
        cancelled.OrderDetails.Add(cancelledDetail);

        var pending = new Order
        {
            User = user,
            OrderDate = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            Status = OrderStatus.Pending,
            TotalAmount = 15m,
            ShippingAddress = "عنوان غير معروف"
        };
        var pendingDetail = new OrderDetail { Product = productB, Quantity = 1, UnitPrice = 15m };
        pending.OrderDetails.Add(pendingDetail);

        _context.Orders.AddRange(_paid2026, _delivered2025, _paidAlexandria2026, cancelled, pending);
        _context.SaveChanges();

        // Only earnings for confirmed orders count. The cancelled order's earning
        // is deliberately present to prove it is ignored.
        _context.PlatformEarnings.AddRange(
            new PlatformEarning
            {
                OrderId = _paid2026.Id,
                OrderDetailId = _paidProductA.Id,
                Amount = 10m,
                Rate = 0.10m
            },
            new PlatformEarning
            {
                OrderId = _paid2026.Id,
                OrderDetailId = _paidProductB.Id,
                Amount = 2m,
                Rate = 0.10m
            },
            new PlatformEarning
            {
                OrderId = _delivered2025.Id,
                OrderDetailId = _deliveredProductA.Id,
                Amount = 5m,
                Rate = 0.10m
            },
            new PlatformEarning
            {
                OrderId = _paidAlexandria2026.Id,
                OrderDetailId = _alexProductB.Id,
                Amount = 2m,
                Rate = 0.10m
            },
            new PlatformEarning
            {
                OrderId = cancelled.Id,
                OrderDetailId = cancelledDetail.Id,
                Amount = 4m,
                Rate = 0.10m
            });

        _context.PlatformExpenses.AddRange(
            new PlatformExpense
            {
                Title = "Hosting 2026",
                Category = "استضافة",
                Amount = 2m,
                ExpenseDate = new DateTime(2026, 11, 1)
            },
            new PlatformExpense
            {
                Title = "Marketing 2025",
                Category = "تسويق",
                Amount = 1m,
                ExpenseDate = new DateTime(2025, 12, 1)
            });
        _context.SaveChanges();

        _unitOfWork = new UnitOfWork(_context);
        _service = new AdminAnalyticsService(_unitOfWork, Mock.Of<ILogger<AdminAnalyticsService>>());
    }

    [Fact]
    public async Task GetDashboardAnalyticsAsync_FiltersConfirmedOrdersAndYear_ComputesProfit()
    {
        var result = await _service.GetDashboardAnalyticsAsync(2026);

        Assert.Equal(2026, result.Year);
        Assert.Equal(125m, result.TotalSales);
        Assert.Equal(140m, result.GrossItemValue);
        Assert.Equal(14m, result.PlatformRevenue);
        Assert.Equal(126m, result.VendorPayable);
        Assert.Equal(2m, result.TotalExpenses);
        Assert.Equal(10m, result.Discounts);
        Assert.Equal(2m, result.NetProfit);
        Assert.Equal(2, result.ConfirmedOrders);
        Assert.Single(result.Expenses);
        Assert.Equal("Hosting 2026", result.Expenses[0].Title);
        Assert.Equal(new[] { 2026, 2025 }, result.AvailableYears.Select(y => y.Year));

        Assert.Collection(result.BestSellingProducts,
            first =>
            {
                Assert.Equal("Product A", first.ProductName);
                Assert.Equal(2, first.UnitsSold);
                Assert.Equal(100m, first.Revenue);
            },
            second =>
            {
                Assert.Equal("Product B", second.ProductName);
                Assert.Equal(2, second.UnitsSold);
                Assert.Equal(40m, second.Revenue);
            });

        Assert.Equal("القاهرة", result.TopRegions[0].Region);
        Assert.Equal(105m, result.TopRegions[0].Sales);
        Assert.Equal("الإسكندرية", result.BottomRegions[0].Region);
        Assert.Equal(20m, result.BottomRegions[0].Sales);
        Assert.Equal(0, result.UnknownRegionOrders);
    }

    [Fact]
    public async Task GetDashboardAnalyticsAsync_AllYears_IncludesEveryConfirmedOrder()
    {
        var result = await _service.GetDashboardAnalyticsAsync(null);

        Assert.Null(result.Year);
        Assert.Equal(175m, result.TotalSales);
        Assert.Equal(190m, result.GrossItemValue);
        Assert.Equal(19m, result.PlatformRevenue);
        Assert.Equal(3m, result.TotalExpenses);
        Assert.Equal(6m, result.NetProfit);
        Assert.Equal(3, result.ConfirmedOrders);
        Assert.Equal(2, result.Expenses.Count);
        Assert.Equal(2, result.AvailableYears.Count);
    }

    [Fact]
    public async Task GetDashboardAnalyticsAsync_ExpenseOnlyYear_IsAvailableAndFiltersExpenses()
    {
        _context.PlatformExpenses.Add(new PlatformExpense
        {
            Title = "Expense only 2023",
            Category = "أخرى",
            Amount = 3m,
            ExpenseDate = new DateTime(2023, 4, 1)
        });
        await _context.SaveChangesAsync();

        var all = await _service.GetDashboardAnalyticsAsync(null);
        Assert.Contains(all.AvailableYears, y => y.Year == 2023);

        var result = await _service.GetDashboardAnalyticsAsync(2023);

        Assert.Equal(2023, result.Year);
        Assert.Equal(0m, result.TotalSales);
        Assert.Equal(3m, result.TotalExpenses);
        Assert.Single(result.Expenses);
        Assert.Equal("Expense only 2023", result.Expenses[0].Title);
    }

    [Fact]
    public async Task GetDashboardAnalyticsAsync_InvalidYear_FallsBackToAllYears()
    {
        var result = await _service.GetDashboardAnalyticsAsync(2024);

        Assert.Null(result.Year);
        Assert.Equal(175m, result.TotalSales);
    }

    [Fact]
    public async Task AddExpenseAsync_ValidData_SavesExpense()
    {
        var result = await _service.AddExpenseAsync(
            "  اختبار  ", "تسويق", 12.345m, new DateTime(2026, 12, 5), "ملاحظة", "admin-id");

        Assert.True(result.Success);
        var expense = await _context.PlatformExpenses
            .OrderByDescending(e => e.Id)
            .FirstAsync();
        Assert.Equal("اختبار", expense.Title);
        Assert.Equal("تسويق", expense.Category);
        Assert.Equal(12.35m, expense.Amount);
        Assert.Equal("admin-id", expense.CreatedByUserId);
    }

    [Theory]
    [InlineData("", "تسويق", 10)]
    [InlineData("مصروف", "", 10)]
    [InlineData("مصروف", "تسويق", 0)]
    public async Task AddExpenseAsync_InvalidData_ReturnsFailure(string title, string category, decimal amount)
    {
        var result = await _service.AddExpenseAsync(title, category, amount, new DateTime(2026, 12, 5), null, null);

        Assert.False(result.Success);
        Assert.Empty(await _context.PlatformExpenses.Where(e => e.Title == title).ToListAsync());
    }

    [Fact]
    public async Task DeleteExpenseAsync_RemovesExpense()
    {
        var expense = new PlatformExpense
        {
            Title = "To delete",
            Category = "أخرى",
            Amount = 1m,
            ExpenseDate = new DateTime(2026, 1, 1)
        };
        _context.PlatformExpenses.Add(expense);
        await _context.SaveChangesAsync();

        var result = await _service.DeleteExpenseAsync(expense.Id);

        Assert.True(result.Success);
        Assert.Null(await _context.PlatformExpenses.FindAsync(expense.Id));
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _context.Dispose();
        _connection.Dispose();
    }
}
