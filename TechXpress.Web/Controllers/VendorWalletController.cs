using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechXpress.Services.Interfaces;
using TechXpress.Web.Models;

namespace TechXpress.Web.Controllers;

[Authorize(Roles = "Vendor")]
public class VendorWalletController : Controller
{
    private readonly IWalletService _walletService;

    public VendorWalletController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    // الـ Id بييجي من الـ Claims، مش من الـ Form
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var summary = await _walletService.GetWalletSummaryAsync(CurrentUserId);
        if (summary == null) return Forbid();

        var model = new VendorWalletViewModel
        {
            StoreName = summary.StoreName,
            Balance = summary.Balance,
            Transactions = summary.Transactions.Select(t => new WalletTransactionItem
            {
                Date = t.CreatedAt,
                Type = t.Type.ToString(),
                Status = t.Status.ToString(),
                Amount = t.Amount,
                BalanceAfter = t.BalanceAfter,
                Description = t.Description,
                RejectionReason = t.RejectionReason,
                ProcessedAt = t.ProcessedAtUtc
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestPayout(PayoutInputModel input)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "برجاء إدخال مبلغ صحيح وبيانات استلام المبلغ.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _walletService.RequestPayoutForUserAsync(
            CurrentUserId, input.Amount, input.PayoutDetails, input.Note);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}