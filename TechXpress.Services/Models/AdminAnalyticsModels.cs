namespace TechXpress.Services.Models;

/// <summary>ملخص مبيعات سنة معينة، يُستخدم في فلتر لوحة الإدارة.</summary>
public record SalesYearDto(int Year, decimal TotalSales);

/// <summary>منتج ضمن قائمة الأكثر مبيعًا.</summary>
public record BestSellingProductDto(
    int ProductId,
    string ProductName,
    int UnitsSold,
    decimal Revenue);

/// <summary>مبيعات منطقة/محافظة، محسوبة من عنوان شحن الطلبات المؤكدة.</summary>
public record RegionSalesDto(
    string Region,
    int Orders,
    decimal Sales);

/// <summary>مصروف تشغيلي مسجّل على المنصة.</summary>
public record PlatformExpenseDto(
    int Id,
    string Title,
    string Category,
    decimal Amount,
    DateTime ExpenseDate,
    string? Notes);

/// <summary>كل مؤشرات لوحة الإدارة المالية والتحليلية بعد تطبيق فلتر السنة.</summary>
public record AdminDashboardAnalyticsDto(
    int? Year,
    IReadOnlyList<SalesYearDto> AvailableYears,
    decimal TotalSales,
    decimal PlatformRevenue,
    decimal VendorPayable,
    decimal TotalExpenses,
    decimal Discounts,
    decimal NetProfit,
    int ConfirmedOrders,
    int UnknownRegionOrders,
    IReadOnlyList<BestSellingProductDto> BestSellingProducts,
    IReadOnlyList<RegionSalesDto> TopRegions,
    IReadOnlyList<RegionSalesDto> BottomRegions,
    IReadOnlyList<PlatformExpenseDto> RecentExpenses);
