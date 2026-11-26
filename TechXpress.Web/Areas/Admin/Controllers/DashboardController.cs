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
    private readonly IAdminAnalyticsService _analyticsService;

    public DashboardController(
        ICategoryService categoryService,
        IProductService productService,
        IOrderService orderService,
        IVendorService vendorService,
        IWalletService walletService,
        IUserManagementService userManagementService,
        IAdminAnalyticsService analyticsService)
    {
        _categoryService = categoryService;
        _productService = productService;
        _orderService = orderService;
        _vendorService = vendorService;
        _walletService = walletService;
        _userManagementService = userManagementService;
        _analyticsService = analyticsService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year)
    {
        var categories = (await _categoryService.GetAllAsync()).ToList();
        var (products, totalProducts) = await _productService.GetPagedAsync(pageSize: 1);
        var orders = (await _orderService.GetAllOrdersAsync()).ToList();
        var vendors = (await _vendorService.GetRequestsAsync(null)).ToList();
        var pendingPayouts = (await _walletService.GetPendingPayoutRequestsAsync()).ToList();
        var users = await _userManagementService.GetAllUsersAsync();
        var analytics = await _analyticsService.GetDashboardAnalyticsAsync(year);

        var model = new DashboardViewModel
        {
            TotalCategories = categories.Count,
            TotalProducts = totalProducts,
            TotalOrders = orders.Count,
            TotalSales = analytics.TotalSales,
            PlatformRevenue = analytics.PlatformRevenue,
            PaidOrders = analytics.ConfirmedOrders,
            TotalVendors = vendors.Count,
            PendingVendors = vendors.Count(v => v.Status == VendorStatus.Pending),
            ApprovedVendors = vendors.Count(v => v.Status == VendorStatus.Approved),
            RejectedVendors = vendors.Count(v => v.Status == VendorStatus.Rejected),
            PendingPayouts = pendingPayouts.Count,
            TotalCustomers = users.Count(u => u.Roles.Contains("Customer")),
            RecentOrders = orders.OrderByDescending(o => o.OrderDate).Take(5),
            SelectedYear = analytics.Year,
            AvailableYears = analytics.AvailableYears,
            VendorPayable = analytics.VendorPayable,
            TotalExpenses = analytics.TotalExpenses,
            Discounts = analytics.Discounts,
            NetProfit = analytics.NetProfit,
            UnknownRegionOrders = analytics.UnknownRegionOrders,
            BestSellingProducts = analytics.BestSellingProducts,
            TopRegions = analytics.TopRegions,
            BottomRegions = analytics.BottomRegions,
            RecentExpenses = analytics.RecentExpenses
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddExpense(
        string title, string category, decimal amount, DateTime expenseDate, string? notes, int? year)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _analyticsService.AddExpenseAsync(title, category, amount, expenseDate, notes, userId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index), new { year });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteExpense(int id, int? year)
    {
        var result = await _analyticsService.DeleteExpenseAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index), new { year });
    }
}
