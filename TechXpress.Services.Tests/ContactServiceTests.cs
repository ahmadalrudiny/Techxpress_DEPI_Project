using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TechXpress.Data.Context;
using TechXpress.Data.Entities;
using TechXpress.Data.UoW;
using TechXpress.Services.Implementations;

namespace TechXpress.Services.Tests;

public class ContactServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _uow;
    private readonly ContactService _service;

    public ContactServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _uow = new UnitOfWork(_context);
        _service = new ContactService(_uow, Mock.Of<ILogger<ContactService>>());
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private async Task<ContactMessage> SeedMessageAsync(string subject, DateTime createdAt, bool isRead = false)
    {
        var message = new ContactMessage
        {
            FullName = "عميل تجريبي",
            Email = "customer@test.com",
            Subject = subject,
            Message = "محتوى الرسالة التجريبية للتأكد من الحفظ والاسترجاع",
            CreatedAt = createdAt,
            IsRead = isRead
        };
        _context.ContactMessages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }

    [Fact]
    public async Task SendAsync_ValidMessage_SavesAndReturnsSuccess()
    {
        var (success, message) = await _service.SendAsync(
            "أحمد محمد", "ahmed@test.com", "01000000000", "استفسار عن طلب", "أريد معرفة موعد وصول الطلب رقم 100.");

        Assert.True(success);
        Assert.Contains("تم استلام رسالتك", message);

        var saved = await _context.ContactMessages.SingleAsync();
        Assert.Equal("أحمد محمد", saved.FullName);
        Assert.Equal("ahmed@test.com", saved.Email);
        Assert.Equal("01000000000", saved.Phone);
        Assert.Equal("استفسار عن طلب", saved.Subject);
        Assert.False(saved.IsRead);
    }

    [Fact]
    public async Task SendAsync_WithoutPhone_StoresNullPhone()
    {
        await _service.SendAsync("أحمد", "a@test.com", "   ", "موضوع", "رسالة كافية الطول للاختبار.");

        var saved = await _context.ContactMessages.SingleAsync();
        Assert.Null(saved.Phone);
    }

    [Theory]
    [InlineData("", "a@test.com", "موضوع", "رسالة كافية الطول")]
    [InlineData("أحمد", "", "موضوع", "رسالة كافية الطول")]
    [InlineData("أحمد", "a@test.com", "", "رسالة كافية الطول")]
    [InlineData("أحمد", "a@test.com", "موضوع", "")]
    public async Task SendAsync_MissingRequiredField_ReturnsError(string name, string email, string subject, string body)
    {
        var (success, message) = await _service.SendAsync(name, email, null, subject, body);

        Assert.False(success);
        Assert.Contains("إكمال جميع الحقول", message);
        Assert.Equal(0, await _context.ContactMessages.CountAsync());
    }

    [Fact]
    public async Task SendAsync_TooLongValues_AreClampedToColumnLimits()
    {
        var longName = new string('A', 400);
        var longSubject = new string('B', 500);
        var longBody = new string('C', 5000);

        var (success, _) = await _service.SendAsync(longName, "a@test.com", null, longSubject, longBody);

        Assert.True(success);
        var saved = await _context.ContactMessages.SingleAsync();
        Assert.Equal(150, saved.FullName.Length);
        Assert.Equal(200, saved.Subject.Length);
        Assert.Equal(2000, saved.Message.Length);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsNewestFirst()
    {
        await SeedMessageAsync("الأقدم", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        await SeedMessageAsync("الأحدث", new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = await _service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("الأحدث", result[0].Subject);
        Assert.Equal("الأقدم", result[1].Subject);
    }

    [Fact]
    public async Task GetUnreadCountAsync_CountsOnlyUnread()
    {
        await SeedMessageAsync("جديدة", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        await SeedMessageAsync("قديمة", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), isRead: true);

        var count = await _service.GetUnreadCountAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task MarkAsReadAsync_ExistingMessage_FlagsAsRead()
    {
        var message = await SeedMessageAsync("موضوع", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var (success, result) = await _service.MarkAsReadAsync(message.Id);

        Assert.True(success);
        Assert.Contains("تم تعليم الرسالة", result);
        Assert.True(await _context.ContactMessages.Where(c => c.Id == message.Id).Select(c => c.IsRead).SingleAsync());
    }

    [Fact]
    public async Task MarkAsReadAsync_MissingMessage_ReturnsError()
    {
        var (success, message) = await _service.MarkAsReadAsync(999);

        Assert.False(success);
        Assert.Contains("غير موجودة", message);
    }

    [Fact]
    public async Task DeleteAsync_ExistingMessage_RemovesIt()
    {
        var message = await SeedMessageAsync("للحذف", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var (success, result) = await _service.DeleteAsync(message.Id);

        Assert.True(success);
        Assert.Contains("تم حذف الرسالة", result);
        Assert.Equal(0, await _context.ContactMessages.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_MissingMessage_ReturnsError()
    {
        var (success, message) = await _service.DeleteAsync(999);

        Assert.False(success);
        Assert.Contains("غير موجودة", message);
    }
}
