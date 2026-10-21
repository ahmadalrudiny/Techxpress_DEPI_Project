using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TechXpress.Data.Entities;
using TechXpress.Data.UoW;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;


namespace TechXpress.Services.Implementations;

public class WalletService : IWalletService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WalletService> _logger;
    private readonly decimal _commissionRate;

    public WalletService(IUnitOfWork unitOfWork, ILogger<WalletService> logger, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _commissionRate = configuration.GetValue<decimal?>("Platform:CommissionRate") ?? 0.10m;
    }

    public async Task ProcessOrderCommissionAsync(int orderId)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null || order.Status != OrderStatus.Paid)
        {
            _logger.LogWarning("Commission skipped: order {OrderId} not found or not paid", orderId);
            return;
        }

        var items = await _unitOfWork.OrderDetails
            .FindAsync(d => d.OrderId == orderId && d.VendorId != null);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            foreach (var item in items)
            {
                if (await _unitOfWork.Wallets.HasDepositForOrderDetailAsync(item.Id))
                    continue;

                var vendor = await _unitOfWork.Vendors.GetByIdAsync(item.VendorId!.Value);
                if (vendor == null) continue;

                var gross = item.UnitPrice * item.Quantity;
                var commission = Math.Round(gross * _commissionRate, 2, MidpointRounding.AwayFromZero);
                var net = gross - commission;

                vendor.WalletBalance += net;

                await _unitOfWork.Wallets.AddAsync(new WalletTransaction
                {
                    VendorId = vendor.Id,
                    OrderDetailId = item.Id,
                    Amount = net,
                    BalanceAfter = vendor.WalletBalance,
                    Type = WalletTransactionType.Deposit,
                    Status = WalletTransactionStatus.Completed,
                    Description = $"أرباح الطلب #{orderId} بعد خصم العمولة"
                });
            }

            await _unitOfWork.CommitAsync();
            _logger.LogInformation("Commission processed for order {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Failed to process commission for order {OrderId}", orderId);
            throw;
        }
    }

    public async Task ReverseOrderCommissionAsync(int orderId)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null || order.Status != OrderStatus.Paid)
        {
            _logger.LogWarning("Reverse commission skipped: order {OrderId} not found or not paid", orderId);
            return;
        }

        var items = await _unitOfWork.OrderDetails
            .FindAsync(d => d.OrderId == orderId && d.VendorId != null);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            foreach (var item in items)
            {
                var deposit = (await _unitOfWork.Wallets
                    .FindAsync(t => t.OrderDetailId == item.Id
                                 && t.Type == WalletTransactionType.Deposit
                                 && t.Status == WalletTransactionStatus.Completed))
                    .FirstOrDefault();

                if (deposit == null) continue;

                var vendor = await _unitOfWork.Vendors.GetByIdAsync(deposit.VendorId);
                if (vendor == null) continue;

                vendor.WalletBalance -= deposit.Amount;

                await _unitOfWork.Wallets.AddAsync(new WalletTransaction
                {
                    VendorId = vendor.Id,
                    OrderDetailId = item.Id,
                    Amount = deposit.Amount,
                    BalanceAfter = vendor.WalletBalance,
                    Type = WalletTransactionType.Refund,
                    Status = WalletTransactionStatus.Completed,
                    Description = $"Reverse commission for order #{orderId} after cancellation"
                });
            }

            await _unitOfWork.CommitAsync();
            _logger.LogInformation("Commission reversed for order {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Failed to reverse commission for order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<(bool Success, string Message)> RequestPayoutAsync(
        int vendorId, decimal amount, string payoutDetails, string? note = null)
    {
        if (amount <= 0)
            return (false, "مبلغ السحب يجب أن يكون أكبر من الصفر.");
        if (string.IsNullOrWhiteSpace(payoutDetails))
            return (false, "برجاء إدخال بيانات استلام المبلغ (رقم المحفظة).");

        var vendor = await _unitOfWork.Vendors.GetByIdAsync(vendorId);
        if (vendor == null)
            return (false, "المورد غير موجود.");
        if (vendor.Status != VendorStatus.Approved)
            return (false, "حساب المورد غير معتمد.");
        if (vendor.WalletBalance < amount)
            return (false, "رصيد المحفظة الحالي غير كافٍ لإتمام طلب السحب.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            vendor.WalletBalance -= amount;   // حجز المبلغ

            await _unitOfWork.Wallets.AddAsync(new WalletTransaction
            {
                VendorId = vendorId,
                Amount = amount,
                BalanceAfter = vendor.WalletBalance,
                Type = WalletTransactionType.Withdrawal,
                Status = WalletTransactionStatus.Pending,
                PayoutDetails = Clamp(payoutDetails.Trim(), 100),
                Description = string.IsNullOrWhiteSpace(note) ? "طلب سحب أرباح" : Clamp(note.Trim(), 300)
            });

            await _unitOfWork.CommitAsync();
            _logger.LogInformation("Payout requested by vendor {VendorId}: {Amount}", vendorId, amount);
            return (true, "تم تقديم طلب سحب الأرباح بنجاح وهو قيد مراجعة الأدمن.");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while requesting payout for vendor {VendorId}", vendorId);
            await _unitOfWork.RollbackAsync();
            return (false, "تم تعديل الرصيد أثناء العملية، حاول مرة أخرى.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Failed to request payout for vendor {VendorId}", vendorId);
            throw;
        }
    }

    public async Task<(bool Success, string Message)> ProcessPayoutAsync(
        int transactionId, bool approve, string adminUserId,
        string? rejectionReason = null, string? adminName = null)
    {
        var tx = await _unitOfWork.Wallets.GetByIdWithVendorAsync(transactionId);
        if (tx == null
            || tx.Type != WalletTransactionType.Withdrawal
            || tx.Status != WalletTransactionStatus.Pending)
            return (false, "طلب السحب غير موجود أو تم معالجته سابقاً.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // سجل تدقيق: مين اتخذ القرار وامتى
            tx.ProcessedAtUtc = DateTime.UtcNow;
            tx.ProcessedByUserId = adminUserId;
            tx.ProcessedByName = adminName;

            if (approve)
            {
                tx.Status = WalletTransactionStatus.Completed;
                tx.RejectionReason = null;
                tx.Description = Clamp($"{tx.Description} | تم اعتماد الطلب", 300);
            }
            else
            {
                tx.Status = WalletTransactionStatus.Rejected;
                tx.RejectionReason = string.IsNullOrWhiteSpace(rejectionReason)
                    ? "بدون سبب محدد"
                    : Clamp(rejectionReason.Trim(), 500);
                tx.Description = Clamp($"{tx.Description} | مرفوض: {tx.RejectionReason}", 300);
                tx.Vendor.WalletBalance += tx.Amount;   // إرجاع المبلغ المحجوز

                await _unitOfWork.Wallets.AddAsync(new WalletTransaction
                {
                    VendorId = tx.VendorId,
                    Amount = tx.Amount,
                    BalanceAfter = tx.Vendor.WalletBalance,
                    Type = WalletTransactionType.Refund,
                    Status = WalletTransactionStatus.Completed,
                    Description = $"إرجاع مبلغ طلب السحب المرفوض #{tx.Id}"
                });
            }

            await _unitOfWork.CommitAsync();
            _logger.LogInformation("Payout {TransactionId} {Decision} by admin {AdminId}",
                transactionId, approve ? "approved" : "rejected", adminUserId);
            return (true, approve ? "تمت الموافقة على طلب السحب." : "تم رفض طلب السحب وإرجاع المبلغ للمحفظة.");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while processing payout {TransactionId}", transactionId);
            await _unitOfWork.RollbackAsync();
            return (false, "تمت معالجة الطلب من أدمن آخر، حدّث الصفحة.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Failed to process payout {TransactionId}", transactionId);
            throw;
        }
    }

    public async Task<IEnumerable<WalletTransaction>> GetVendorTransactionsAsync(int vendorId)
        => await _unitOfWork.Wallets.GetTransactionsByVendorIdAsync(vendorId);

    public async Task<IEnumerable<WalletTransaction>> GetPendingPayoutRequestsAsync()
        => await _unitOfWork.Wallets.GetPendingPayoutRequestsAsync();
    public async Task<IReadOnlyList<PayoutRequestItem>> GetPayoutRequestsAsync(WalletTransactionStatus? status)
    {
        var requests = await _unitOfWork.Wallets.GetPayoutRequestsAsync(status);

        return requests.Select(t => new PayoutRequestItem(
            t.Id,
            t.VendorId,
            t.Vendor?.StoreName ?? "-",
            t.Amount,
            t.PayoutDetails,
            t.Description,
            t.Status,
            t.CreatedAt,
            t.ProcessedAtUtc,
            t.ProcessedByName,
            t.RejectionReason)).ToList();
    }

    public async Task<PayoutCounts> GetPayoutRequestCountsAsync()
    {
        var (total, pending, approved, rejected) = await _unitOfWork.Wallets.GetPayoutRequestCountsAsync();
        return new PayoutCounts(total, pending, approved, rejected);
    }

    public async Task<VendorWalletSummary?> GetWalletSummaryAsync(string userId)
    {
        var vendor = await _unitOfWork.Vendors.GetByUserIdAsync(userId);
        if (vendor == null || vendor.Status != VendorStatus.Approved)
            return null;

        var transactions = (await _unitOfWork.Wallets.GetTransactionsByVendorIdAsync(vendor.Id)).ToList();
        return new VendorWalletSummary(vendor.Id, vendor.StoreName, vendor.WalletBalance, transactions);
    }

    public async Task<(bool Success, string Message)> RequestPayoutForUserAsync(
        string userId, decimal amount, string payoutDetails, string? note = null)
    {
        var vendor = await _unitOfWork.Vendors.GetByUserIdAsync(userId);
        if (vendor == null)
            return (false, "حساب المورد غير موجود.");

        return await RequestPayoutAsync(vendor.Id, amount, payoutDetails, note);
    }

    /// <summary>Cuts a text value down to the maximum length allowed by the column.</summary>
    private static string? Clamp(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength) return value;
        return value[..maxLength];
    }
}