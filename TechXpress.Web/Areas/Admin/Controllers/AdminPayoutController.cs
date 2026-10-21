using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechXpress.Data.Entities;
using TechXpress.Services.Interfaces;
using TechXpress.Web.Areas.Admin.Models;

namespace TechXpress.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AdminPayoutController : Controller
{
    private readonly IWalletService _walletService;

    public AdminPayoutController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    /// <summary>
    /// Payout request history with an optional status filter
    /// (Pending = awaiting review, Completed = approved/paid, Rejected = refused).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(WalletTransactionStatus? status = null)
    {
        var requests = await _walletService.GetPayoutRequestsAsync(status);
        var counts = await _walletService.GetPayoutRequestCountsAsync();

        var model = new PayoutHistoryViewModel
        {
            StatusFilter = status,
            Counts = counts,
            Requests = requests.Select(t => new PayoutRequestViewModel
            {
                TransactionId = t.Id,
                VendorId = t.VendorId,
                StoreName = t.StoreName,
                Amount = t.Amount,
                PayoutDetails = t.PayoutDetails,
                Note = t.Note,
                Status = t.Status,
                RequestedAt = t.RequestedAt,
                ProcessedAt = t.ProcessedAtUtc,
                ProcessedByName = t.ProcessedByName,
                RejectionReason = t.RejectionReason
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ApprovePayout(int transactionId, WalletTransactionStatus? status = null)
        => ProcessAsync(transactionId, approve: true, reason: null, status);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> RejectPayout(int transactionId, string? reason, WalletTransactionStatus? status = null)
        => ProcessAsync(transactionId, approve: false, reason, status);

    private async Task<IActionResult> ProcessAsync(
        int transactionId, bool approve, string? reason, WalletTransactionStatus? status)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var adminName = User.Identity?.Name;

        var result = await _walletService.ProcessPayoutAsync(transactionId, approve, adminId, reason, adminName);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index), new { status });
    }
}
