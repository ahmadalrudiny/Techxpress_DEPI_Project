using Microsoft.Extensions.Logging;
using TechXpress.Data.Repositories.Interfaces;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;

namespace TechXpress.Services.Implementations;

public class UserManagementService : IUserManagementService
{
    private const string AdminRole = "Admin";

    private readonly IUserManagementRepository _repository;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        IUserManagementRepository repository,
        ILogger<UserManagementService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AdminUserDto>> GetAllUsersAsync()
    {
        var users = await _repository.GetAllUsersWithRolesAsync();

        return users.Select(u => new AdminUserDto(
            u.User.Id,
            u.User.FullName ?? string.Empty,
            u.User.Email ?? string.Empty,
            u.User.PhoneNumber,
            u.User.City,
            u.User.CreatedAt,
            u.User.IsActive,
            u.Roles)).ToList();
    }

    public async Task<(bool Success, string Message)> ActivateUserAsync(string targetUserId)
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
            return (false, "معرّف المستخدم غير صالح.");

        var updated = await _repository.SetUserActiveAsync(targetUserId, isActive: true);
        if (!updated)
            return (false, "المستخدم غير موجود.");

        _logger.LogInformation("User {UserId} activated", targetUserId);
        return (true, "تم تفعيل حساب المستخدم بنجاح.");
    }

    public async Task<(bool Success, string Message)> SuspendUserAsync(string targetUserId, string currentAdminId)
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
            return (false, "معرّف المستخدم غير صالح.");

        // قاعدة 1: لا يمكن للأدمن تعليق حسابه الخاص
        if (targetUserId == currentAdminId)
            return (false, "لا يمكنك تعليق حسابك الإداري الخاص.");

        // قاعدة 2: لا يمكن تعليق آخر أدمن نشط في النظام
        if (await _repository.IsInRoleAsync(targetUserId, AdminRole))
        {
            var activeAdmins = await _repository.CountActiveAdminsAsync();
            if (activeAdmins <= 1)
                return (false, "لا يمكن تعليق حساب آخر مدير نشط في النظام.");
        }

        var updated = await _repository.SetUserActiveAsync(targetUserId, isActive: false);
        if (!updated)
            return (false, "المستخدم غير موجود.");

        _logger.LogInformation("User {UserId} suspended by admin {AdminId}", targetUserId, currentAdminId);
        return (true, "تم تعليق حساب المستخدم.");
    }
}
