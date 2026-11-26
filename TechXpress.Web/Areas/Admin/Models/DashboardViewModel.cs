using TechXpress.Services.Models;

namespace TechXpress.Web.Areas.Admin.Models;

public class DashboardViewModel
{
    public int TotalProducts { get; set; }
    public int TotalCategories { get; set; }
    public int TotalOrders { get; set; }
    public int TotalVendors { get; set; }
    public int PendingVendors { get; set; }
    public int ApprovedVendors { get; set; }
    public int RejectedVendors { get; set; }
    public int PendingPayouts { get; set; }
    public int TotalCustomers { get; set; }

    /// <summary>إجمالي المبيعات المؤكدة — الطلبات المدفوعة والمشحونة والمُسلّمة فقط.</summary>
    public decimal TotalSales { get; set; }

    /// <summary>إجمالي عمولة الموقع من الطلبات غير الملغاة أو المُرجعة.</summary>
    public decimal PlatformRevenue { get; set; }

    /// <summary>عدد الطلبات المؤكدة المستخدمة في حساب إجمالي المبيعات.</summary>
    public int PaidOrders { get; set; }

    /// <summary>السنة المختارة في فلتر المبيعات، أو null لعرض كل السنوات.</summary>
    public int? SelectedYear { get; set; }

    /// <summary>السنوات المتاحة في الفلتر مع مبيعاتها المؤكدة.</summary>
    public IReadOnlyList<SalesYearDto> AvailableYears { get; set; } = new List<SalesYearDto>();

    /// <summary>إجمالي قيمة البنود المستحقة للبائعين بعد خصم العمولة.</summary>
    public decimal VendorPayable { get; set; }

    /// <summary>إجمالي المصروفات التشغيلية المسجلة في الفترة المختارة.</summary>
    public decimal TotalExpenses { get; set; }

    /// <summary>قيمة الخصومات الممنوحة على الطلبات المؤكدة (تتحملها المنصة).</summary>
    public decimal Discounts { get; set; }

    /// <summary>صافي الربح = العمولة - المصروفات - الخصومات.</summary>
    public decimal NetProfit { get; set; }

    public int UnknownRegionOrders { get; set; }

    public IReadOnlyList<BestSellingProductDto> BestSellingProducts { get; set; } = new List<BestSellingProductDto>();
    public IReadOnlyList<RegionSalesDto> TopRegions { get; set; } = new List<RegionSalesDto>();
    public IReadOnlyList<RegionSalesDto> BottomRegions { get; set; } = new List<RegionSalesDto>();
    public IReadOnlyList<PlatformExpenseDto> RecentExpenses { get; set; } = new List<PlatformExpenseDto>();

    /// <summary>آخر الطلبات لعرضها في لوحة التحكم.</summary>
    public IEnumerable<OrderDto> RecentOrders { get; set; } = new List<OrderDto>();
}
