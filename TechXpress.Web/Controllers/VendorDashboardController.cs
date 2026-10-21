using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechXpress.Data.Entities;
using TechXpress.Services.Interfaces;
using TechXpress.Web.Models;

namespace TechXpress.Web.Controllers;

/// <summary>
/// لوحة تحكم البائع: ملخّص الأرباح والمنتجات وطلبات المتجر في صفحة واحدة.
/// </summary>
[Authorize(Roles = "Vendor")]
public class VendorDashboardController : Controller
{
    private readonly IVendorService _vendorService;
    private readonly IVendorProductService _vendorProductService;
    private readonly IWalletService _walletService;

    public VendorDashboardController(
        IVendorService vendorService,
        IVendorProductService vendorProductService,
        IWalletService walletService)
    {
        _vendorService = vendorService;
        _vendorProductService = vendorProductService;
        _walletService = walletService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var application = await _vendorService.GetApplicationStatusAsync(CurrentUserId);
        if (application == null)
            return RedirectToAction("Apply", "VendorApply");

        var vendorId = application.VendorId;

        var model = new VendorDashboardViewModel
        {
            VendorId = vendorId,
            StoreName = application.StoreName,
            Status = application.Status.ToString(),
            IsApproved = application.Status == VendorStatus.Approved,
            RejectionReason = application.RejectionReason,
            Stats = await _vendorService.GetDashboardStatsAsync(vendorId)
        };

        var wallet = await _walletService.GetWalletSummaryAsync(CurrentUserId);
        if (wallet != null)
        {
            model.Balance = wallet.Balance;
            model.RecentTransactions = wallet.Transactions
                .Take(5)
                .Select(t => new WalletTransactionItem
                {
                    Date = t.CreatedAt,
                    Type = t.Type.ToString(),
                    Status = t.Status.ToString(),
                    Amount = t.Amount,
                    BalanceAfter = t.BalanceAfter,
                    Description = t.Description
                })
                .ToList();
        }

        var (products, _) = await _vendorProductService.GetVendorProductsPagedAsync(vendorId, 1, 4);
        model.RecentProducts = products.ToList();

        var orders = await _vendorService.GetVendorOrdersAsync(vendorId);
        model.RecentOrders = orders.OrderByDescending(o => o.OrderDate).Take(5).ToList();

        return View(model);
    }
}
