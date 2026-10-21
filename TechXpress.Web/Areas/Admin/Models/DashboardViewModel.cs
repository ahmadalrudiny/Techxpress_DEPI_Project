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

    /// <summary>عدد الطلبات المؤكدة المستخدمة في حساب إجمالي المبيعات.</summary>
    public int PaidOrders { get; set; }

    /// <summary>آخر الطلبات لعرضها في لوحة التحكم.</summary>
    public IEnumerable<OrderDto> RecentOrders { get; set; } = new List<OrderDto>();
}
