using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechXpress.Data.Context;

namespace TechXpress.Data.Tests;

public static class TestDbFactory
{
    // داتابيز SQLite في الذاكرة بتدعم الـ Transactions
    public static (ApplicationDbContext Context, SqliteConnection Connection) Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();   // لازم تظل مفتوحة طول الاختبار

        // تعطيل قيود المفاتيح الخارجية في SQLite لسهولة الإعداد في الاختبارات
        using var pragmaCmd = connection.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA foreign_keys = OFF";
        pragmaCmd.ExecuteNonQuery();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return (context, connection);
    }
}