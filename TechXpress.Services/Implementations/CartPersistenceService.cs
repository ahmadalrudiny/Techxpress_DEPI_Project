using Microsoft.Extensions.Logging;
using TechXpress.Data.UoW;
using TechXpress.Services.Interfaces;

namespace TechXpress.Services.Implementations;

public class CartPersistenceService : ICartPersistenceService
{
    /// <summary>حد أقصى دفاعي لحجم JSON المحفوظ (حماية من إساءة الاستخدام).</summary>
    private const int MaxCartJsonLength = 200_000;

    /// <summary>تمثيل السلة الفارغة (تُخزَّن كـ NULL في قاعدة البيانات).</summary>
    private const string EmptyCartJson = "[]";

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CartPersistenceService> _logger;

    public CartPersistenceService(IUnitOfWork unitOfWork, ILogger<CartPersistenceService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<string?> GetSavedCartAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        try
        {
            var cartJson = await _unitOfWork.PersistentCarts.GetCartJsonAsync(userId);
            return string.IsNullOrWhiteSpace(cartJson) ? null : cartJson;
        }
        catch (Exception ex)
        {
            // السلة المحفوظة ميزة راحة؛ لا نُفشل تصفح المتجر بسببها.
            _logger.LogError(ex, "تعذّر قراءة السلة المحفوظة للمستخدم {UserId}", userId);
            return null;
        }
    }

    public async Task SaveCartAsync(string userId, string cartJson)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        // السلة الفارغة تُخزَّن كـ NULL بدلاً من "[]" حتى لا تبقى بيانات بلا معنى في قاعدة البيانات
        if (string.IsNullOrWhiteSpace(cartJson) || cartJson.Trim() == EmptyCartJson)
        {
            await ClearAsync(userId);
            return;
        }

        if (cartJson.Length > MaxCartJsonLength)
        {
            _logger.LogWarning("تم تجاهل حفظ سلة أكبر من الحد المسموح للمستخدم {UserId}", userId);
            return;
        }

        try
        {
            var found = await _unitOfWork.PersistentCarts.SaveCartJsonAsync(userId, cartJson);
            if (found)
            {
                await _unitOfWork.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "تعذّر حفظ سلة المستخدم {UserId}", userId);
        }
    }

    public async Task ClearAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        try
        {
            var found = await _unitOfWork.PersistentCarts.SaveCartJsonAsync(userId, null);
            if (found)
            {
                await _unitOfWork.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "تعذّر حذف السلة المحفوظة للمستخدم {UserId}", userId);
        }
    }
}
