using TechXpress.Data.Entities;

namespace TechXpress.Data.Repositories.Interfaces;

/// <summary>مستخدم مع أدواره (بدون الاعتماد على أنواع ASP.NET Identity في طبقة الخدمات).</summary>
public record UserWithRoles(ApplicationUser User, IReadOnlyList<string> Roles);

public interface IUserManagementRepository
{
    /// <summary>كل المستخدمين مع أدوارهم، الأحدث تسجيلاً أولاً.</summary>
    Task<IReadOnlyList<UserWithRoles>> GetAllUsersWithRolesAsync();

    /// <summary>تفعيل/تعليق مستخدم. عند التعليق يُبطَل أمنياً (SecurityStamp).</summary>
    Task<bool> SetUserActiveAsync(string userId, bool isActive);

    /// <summary>عدد المدراء النشطين حالياً.</summary>
    Task<int> CountActiveAdminsAsync();

    /// <summary>هل هذا المستخدم يحمل الدور المحدد؟</summary>
    Task<bool> IsInRoleAsync(string userId, string roleName);
}
