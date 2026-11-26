using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechXpress.Data.Entities;
using TechXpress.Data.UoW;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;

namespace TechXpress.Services.Implementations;

/// <summary>
/// يحسب مؤشرات لوحة الإدارة من الطلبات المؤكدة فقط (Paid / Shipped / Delivered)،
/// ويطبّق فلتر السنة على المبيعات والعمولة والمصروفات والتحليلات معًا.
/// </summary>
public class AdminAnalyticsService : IAdminAnalyticsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminAnalyticsService> _logger;

    private static readonly HashSet<OrderStatus> ConfirmedStatuses = new()
    {
        OrderStatus.Paid,
        OrderStatus.Shipped,
        OrderStatus.Delivered
    };

    public AdminAnalyticsService(IUnitOfWork unitOfWork, ILogger<AdminAnalyticsService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AdminDashboardAnalyticsDto> GetDashboardAnalyticsAsync(int? year)
    {
        var orders = (await _unitOfWork.Orders.GetAllWithDetailsForAnalyticsAsync()).ToList();

        var availableYears = orders
            .GroupBy(o => o.OrderDate.Year)
            .Select(g => new SalesYearDto(
                g.Key,
                g.Where(o => ConfirmedStatuses.Contains(o.Status))
                    .Sum(o => o.TotalAmount + o.StoreCreditUsed)))
            .OrderByDescending(y => y.Year)
            .ToList();

        if (availableYears.Count == 0)
        {
            availableYears.Add(new SalesYearDto(DateTime.UtcNow.Year, 0m));
        }

        // سنة غير موجودة أو غير صالحة = كل السنوات.
        var selectedYear = year.HasValue && availableYears.Any(y => y.Year == year.Value)
            ? year
            : null;

        var periodOrders = selectedYear.HasValue
            ? orders.Where(o => o.OrderDate.Year == selectedYear.Value).ToList()
            : orders;

        var confirmedOrders = periodOrders
            .Where(o => ConfirmedStatuses.Contains(o.Status))
            .ToList();

        var confirmedOrderIds = confirmedOrders.Select(o => o.Id).ToHashSet();

        var totalSales = confirmedOrders.Sum(o => o.TotalAmount + o.StoreCreditUsed);
        var discounts = confirmedOrders.Sum(o => o.DiscountAmount);
        var grossItemValue = confirmedOrders
            .SelectMany(o => o.OrderDetails)
            .Sum(d => d.UnitPrice * d.Quantity);

        // العمولة الفعلية المسجلة لكل بند، مع استبعاد العمولات المعكوسة.
        var earnings = await _unitOfWork.PlatformEarnings.Query()
            .AsNoTracking()
            .Where(e => !e.IsReversed && confirmedOrderIds.Contains(e.OrderId))
            .ToListAsync();
        var platformRevenue = earnings.Sum(e => e.Amount);
        var vendorPayable = Math.Max(0m, grossItemValue - platformRevenue);

        var expensesQuery = _unitOfWork.PlatformExpenses.Query().AsNoTracking();
        if (selectedYear.HasValue)
        {
            expensesQuery = expensesQuery.Where(e => e.ExpenseDate.Year == selectedYear.Value);
        }

        var expenses = await expensesQuery
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.Id)
            .ToListAsync();

        var totalExpenses = expenses.Sum(e => e.Amount);
        var netProfit = platformRevenue - totalExpenses - discounts;

        var bestSellingProducts = confirmedOrders
            .SelectMany(o => o.OrderDetails)
            .GroupBy(d => new { d.ProductId, Name = d.Product?.Name })
            .Select(g => new BestSellingProductDto(
                g.Key.ProductId,
                string.IsNullOrWhiteSpace(g.Key.Name) ? $"منتج #{g.Key.ProductId}" : g.Key.Name,
                g.Sum(d => d.Quantity),
                g.Sum(d => d.UnitPrice * d.Quantity)))
            .OrderByDescending(p => p.UnitsSold)
            .ThenByDescending(p => p.Revenue)
            .Take(8)
            .ToList();

        var regionGroups = confirmedOrders
            .GroupBy(o => EgyptRegionResolver.Resolve(o.ShippingAddress))
            .Select(g => new RegionSalesDto(
                g.Key,
                g.Count(),
                g.Sum(o => o.TotalAmount + o.StoreCreditUsed)))
            .ToList();

        var knownRegions = regionGroups
            .Where(r => r.Region != EgyptRegionResolver.Unknown)
            .ToList();

        var topRegions = knownRegions
            .OrderByDescending(r => r.Sales)
            .ThenByDescending(r => r.Orders)
            .Take(5)
            .ToList();

        var bottomRegions = knownRegions
            .OrderBy(r => r.Sales)
            .ThenBy(r => r.Orders)
            .Take(5)
            .ToList();

        var unknownRegionOrders = regionGroups
            .Where(r => r.Region == EgyptRegionResolver.Unknown)
            .Sum(r => r.Orders);

        return new AdminDashboardAnalyticsDto(
            selectedYear,
            availableYears,
            totalSales,
            platformRevenue,
            vendorPayable,
            totalExpenses,
            discounts,
            netProfit,
            confirmedOrders.Count,
            unknownRegionOrders,
            bestSellingProducts,
            topRegions,
            bottomRegions,
            expenses.Take(8).Select(e => new PlatformExpenseDto(
                e.Id, e.Title, e.Category, e.Amount, e.ExpenseDate, e.Notes)).ToList());
    }

    public async Task<(bool Success, string Message)> AddExpenseAsync(
        string title, string category, decimal amount, DateTime expenseDate,
        string? notes, string? createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(title))
            return (false, "عنوان المصروف مطلوب.");

        if (string.IsNullOrWhiteSpace(category))
            return (false, "تصنيف المصروف مطلوب.");

        if (amount <= 0)
            return (false, "قيمة المصروف يجب أن تكون أكبر من صفر.");

        if (expenseDate == default)
            return (false, "تاريخ المصروف غير صالح.");

        await _unitOfWork.PlatformExpenses.AddAsync(new PlatformExpense
        {
            Title = Clamp(title.Trim(), 150),
            Category = Clamp(category.Trim(), 60),
            Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero),
            ExpenseDate = expenseDate,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : Clamp(notes.Trim(), 500),
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Platform expense recorded: {Title} / {Amount}", title, amount);
        return (true, "تم تسجيل المصروف بنجاح.");
    }

    public async Task<(bool Success, string Message)> DeleteExpenseAsync(int id)
    {
        var expense = await _unitOfWork.PlatformExpenses.GetByIdAsync(id);
        if (expense == null)
            return (false, "المصروف غير موجود.");

        _unitOfWork.PlatformExpenses.Delete(expense);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Platform expense deleted: {ExpenseId}", id);
        return (true, "تم حذف المصروف.");
    }

    private static string Clamp(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}

/// <summary>
/// يستخرج المحافظة/المنطقة من عنوان الشحن الحر. يعتمد على مطابقة أطول اسم/alias
/// لتجنّب الخلط بين المدن والمحافظات (مثال: شبرا الخيمة ليست شبرا).
/// </summary>
internal static class EgyptRegionResolver
{
    public const string Unknown = "غير محدد";

    private static readonly (string Name, string[] Aliases)[] Regions =
    {
        ("القاهرة", new[] { "القاهرة", "القاهره", "مدينة نصر", "مدينه نصر", "مصر الجديدة", "مصر الجديده", "المعادي", "المعادى", "حلوان", "شبرا", "التجمع", "الرحاب", "مدينتي", "مدينتى", "العبور", "الشروق", "بدر", "15 مايو", "القاهرة الجديدة", "القاهره الجديده", "cairo" }),
        ("الجيزة", new[] { "الجيزة", "الجيزه", "الهرم", "الدقي", "الدقى", "المهندسين", "6 أكتوبر", "6 اكتوبر", "أكتوبر", "اكتوبر", "الشيخ زايد", "فيصل", "العجوزة", "العجوزه", "إمبابة", "امبابه", "giza", "october" }),
        ("الإسكندرية", new[] { "الإسكندرية", "الاسكندرية", "اسكندرية", "سموحة", "سموحه", "سيدي جابر", "سيدى جابر", "العجمي", "العجمى", "المنتزة", "المنتزه", "alexandria", "alex" }),
        ("الدقهلية", new[] { "الدقهلية", "الدقهليه", "المنصورة", "المنصوره", "ميت غمر", "طلخا", "دكرنس" }),
        ("الشرقية", new[] { "الشرقية", "الشرقيه", "الزقازيق", "بلبيس", "العاشر من رمضان", "10 رمضان" }),
        ("القليوبية", new[] { "القليوبية", "القليوبيه", "بنها", "شبرا الخيمة", "شبرا الخيمه", "القناطر", "قليوب" }),
        ("المنوفية", new[] { "المنوفية", "المنوفيه", "شبين الكوم", "منوف", "أشمون", "اشمون", "السادات" }),
        ("الغربية", new[] { "الغربية", "الغربيه", "طنطا", "المحلة", "المحله", "كفر الزيات", "كفر الزيات" }),
        ("البحيرة", new[] { "البحيرة", "البحيره", "دمنهور", "كفر الدوار", "رشيد", "إيتاي", "ايتاي" }),
        ("كفر الشيخ", new[] { "كفر الشيخ", "دسوق", "فوه", "بلطيم" }),
        ("دمياط", new[] { "دمياط", "رأس البر", "راس البر" }),
        ("بورسعيد", new[] { "بورسعيد", "بور سعيد" }),
        ("الإسماعيلية", new[] { "الإسماعيلية", "الاسماعيلية", "اسماعيلية", "فايد" }),
        ("السويس", new[] { "السويس", "سويس" }),
        ("شمال سيناء", new[] { "شمال سيناء", "شمال سينا", "العريش" }),
        ("جنوب سيناء", new[] { "جنوب سيناء", "جنوب سينا", "شرم الشيخ", "دهب", "نويبع" }),
        ("البحر الأحمر", new[] { "البحر الأحمر", "البحر الاحمر", "الغردقة", "الغردقه", "سفاجا", "مرسى علم" }),
        ("الفيوم", new[] { "الفيوم", "فيوم", "سنورس" }),
        ("بني سويف", new[] { "بني سويف", "بنى سويف", "بني سويف" }),
        ("المنيا", new[] { "المنيا", "ملوي", "ملوى", "بني مزار" }),
        ("أسيوط", new[] { "أسيوط", "اسيوط", "أبنوب", "ابنوب" }),
        ("سوهاج", new[] { "سوهاج", "طهطا", "جرجا" }),
        ("قنا", new[] { "قنا", "قفط", "نجع حمادي" }),
        ("الأقصر", new[] { "الأقصر", "الاقصر", "أقصر", "اقصر", "إسنا", "اسنا" }),
        ("أسوان", new[] { "أسوان", "اسوان", "كوم أمبو", "كوم امبو" }),
        ("الوادي الجديد", new[] { "الوادي الجديد", "الوادى الجديد", "الخارجة", "الخارجه", "الداخلة" }),
        ("مطروح", new[] { "مطروح", "مرسى مطروح", "مرسي مطروح", "سيوة", "سيوه" })
    };

    private static readonly (string Name, string Alias)[] NormalizedAliases = Regions
        .SelectMany(r => r.Aliases.Select(a => (r.Name, Alias: Normalize(a))))
        .OrderByDescending(x => x.Alias.Length)
        .ToArray();

    public static string Resolve(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return Unknown;

        var normalized = Normalize(address);
        foreach (var (name, alias) in NormalizedAliases)
        {
            if (alias.Length >= 3 && normalized.Contains(alias, StringComparison.Ordinal))
                return name;
        }

        return Unknown;
    }

    private static string Normalize(string value)
        => value
            .Replace('أ', 'ا')
            .Replace('إ', 'ا')
            .Replace('آ', 'ا')
            .Replace('ة', 'ه')
            .Replace('ى', 'ي')
            .Replace("محافظة", " ")
            .Replace("محافظه", " ")
            .Replace("،", " ")
            .Replace(",", " ")
            .Replace("-", " ")
            .Replace("_", " ")
            .Replace("  ", " ")
            .Trim()
            .ToLowerInvariant();
}
