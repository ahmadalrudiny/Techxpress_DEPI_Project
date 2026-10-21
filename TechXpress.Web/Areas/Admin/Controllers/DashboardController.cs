using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechXpress.Data.Entities;
using TechXpress.Services.Interfaces;
using TechXpress.Web.Areas.Admin.Models;

namespace TechXpress.Web.Areas.Admin.Controllers;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class DashboardController : Controller
{
    private readonly ICategoryService _categoryService;
    private readonly IProductService _productService;
    private readonly IOrderService _orderService;
    private readonly IVendorService _vendorService;
    private readonly IWalletService _walletService;
    private readonly IUserManagementService _userManagementService;

    public DashboardController(
        ICategoryService categoryService,
        IProductService productService,
        IOrderService orderService,
        IVendorService vendorService,
        IWalletService walletService,
        IUserManagementService userManagementService)
    {
        _categoryService = categoryService;
        _productService = productService;
        _orderService = orderService;
        _vendorService = vendorService;
        _walletService = walletService;
        _userManagementService = userManagementService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var categories = (await _categoryService.GetAllAsync()).ToList();
        var (products, totalProducts) = await _productService.GetPagedAsync(pageSize: 1);
        var orders = (await _orderService.GetAllOrdersAsync()).ToList();
        var vendors = (await _vendorService.GetRequestsAsync(null)).ToList();
        var pendingPayouts = (await _walletService.GetPendingPayoutRequestsAsync()).ToList();
        var users = await _userManagementService.GetAllUsersAsync();

        // إجمالي المبيعات يحتسب الطلبات المؤكدة فقط (مدفوع / مشحون / مُسلّم).
        // الطلبات غير المدفوعة (قيد الانتظار) والملغاة لا تُحتسب.
        var confirmedOrders = orders
            .Where(o => o.Status is OrderStatus.Paid or OrderStatus.Shipped or OrderStatus.Delivered)
            .ToList();

        var model = new DashboardViewModel
        {
            TotalCategories = categories.Count,
            TotalProducts = totalProducts,
            TotalOrders = orders.Count,
            TotalSales = confirmedOrders.Sum(o => o.TotalAmount),
            PaidOrders = confirmedOrders.Count,
            TotalVendors = vendors.Count,
            PendingVendors = vendors.Count(v => v.Status == VendorStatus.Pending),
            ApprovedVendors = vendors.Count(v => v.Status == VendorStatus.Approved),
            RejectedVendors = vendors.Count(v => v.Status == VendorStatus.Rejected),
            PendingPayouts = pendingPayouts.Count,
            TotalCustomers = users.Count(u => u.Roles.Contains("Customer")),
            RecentOrders = orders.OrderByDescending(o => o.OrderDate).Take(5)
        };

        return View(model);
    }
}
