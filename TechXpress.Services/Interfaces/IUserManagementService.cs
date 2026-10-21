using TechXpress.Services.Models;

namespace TechXpress.Services.Interfaces;

public interface IUserManagementService
{
    /// <summary>Returns all users with their roles.</summary>
    Task<IReadOnlyList<AdminUserDto>> GetAllUsersAsync();

    /// <summary>Activates a suspended user account.</summary>
    Task<(bool Success, string Message)> ActivateUserAsync(string targetUserId);

    /// <summary>
    /// Suspends a user account. Rules enforced: an admin cannot suspend their own
    /// account, and the last active administrator can never be suspended.
    /// </summary>
    Task<(bool Success, string Message)> SuspendUserAsync(string targetUserId, string currentAdminId);
}
