using TechXpress.Services.Models;

namespace TechXpress.Services.Interfaces;

/// <summary>
/// تحليلات لوحة الإدارة: المبيعات حسب السنة، الأكثر مبيعًا، المناطق،
/// المصروفات وصافي الربح. تُطبَّق كل المؤشرات على نفس فلتر السنة.
/// </summary>
public interface IAdminAnalyticsService
{
    /// <summary>يُرجع التحليلات، وإذا كانت السنة غير موجودة يعود إلى كل السنوات.</summary>
    Task<AdminDashboardAnalyticsDto> GetDashboardAnalyticsAsync(int? year);

    /// <summary>يسجّل مصروفًا تشغيليًا جديدًا بعد التحقق من البيانات.</summary>
    Task<(bool Success, string Message)> AddExpenseAsync(
        string title, string category, decimal amount, DateTime expenseDate,
        string? notes, string? createdByUserId);

    /// <summary>يحذف مصروفًا تشغيليًا.</summary>
    Task<(bool Success, string Message)> DeleteExpenseAsync(int id);
}
