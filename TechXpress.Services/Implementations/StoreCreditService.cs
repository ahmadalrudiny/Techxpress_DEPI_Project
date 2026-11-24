using Microsoft.Extensions.Logging;
using TechXpress.Data.Entities;
using TechXpress.Data.UoW;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;

namespace TechXpress.Services.Implementations;

public class StoreCreditService : IStoreCreditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StoreCreditService> _logger;

    public StoreCreditService(IUnitOfWork unitOfWork, ILogger<StoreCreditService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task<decimal> GetBalanceAsync(string userId)
        => _unitOfWork.StoreCredits.GetBalanceAsync(userId);

    public async Task<StoreCreditSummaryDto?> GetSummaryAsync(string userId)
    {
        var user = await _unitOfWork.StoreCredits.GetUserAsync(userId);
        if (user == null) return null;

        var transactions = await _unitOfWork.StoreCredits.GetTransactionsAsync(userId);

        return new StoreCreditSummaryDto(
            user.StoreCreditBalance,
            transactions.Select(t => new StoreCreditTransactionDto(
                t.Id,
                t.Amount,
                t.BalanceAfter,
                t.Type,
                t.OrderId,
                t.Description,
                t.CreatedAt)).ToList());
    }

    public async Task<(bool Success, string Message)> CreditAsync(
        string userId, decimal amount, string description, int? orderId = null)
    {
        if (amount <= 0)
            return (false, "قيمة الرصيد يجب أن تكون أكبر من الصفر.");

        var user = await _unitOfWork.StoreCredits.GetUserAsync(userId);
        if (user == null)
            return (false, "المستخدم غير موجود.");

        user.StoreCreditBalance += amount;

        await _unitOfWork.StoreCredits.AddTransactionAsync(new StoreCreditTransaction
        {
            UserId = userId,
            Amount = amount,
            BalanceAfter = user.StoreCreditBalance,
            Type = StoreCreditTransactionType.AdminAdjustment,
            OrderId = orderId,
            Description = Clamp(description, 300)
        });

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Store credit {Amount} added to user {UserId}", amount, userId);

        return (true, $"تمت إضافة ${amount:F2} إلى رصيد النقاط.");
    }

    private static string Clamp(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return "تعديل رصيد";
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
