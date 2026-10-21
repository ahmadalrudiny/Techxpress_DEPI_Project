using Microsoft.EntityFrameworkCore;
using TechXpress.Data.Context;
using TechXpress.Data.Repositories.Interfaces;

namespace TechXpress.Data.Repositories.Implementations;

public class UserManagementRepository : IUserManagementRepository
{
    private readonly ApplicationDbContext _context;

    public UserManagementRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UserWithRoles>> GetAllUsersWithRolesAsync()
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var rolePairs = await (from ur in _context.UserRoles
                               join r in _context.Roles on ur.RoleId equals r.Id
                               select new { ur.UserId, Name = r.Name! })
                              .ToListAsync();

        var rolesByUser = rolePairs
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.Select(x => x.Name).OrderBy(n => n).ToList());

        return users.Select(u => new UserWithRoles(
            u,
            rolesByUser.TryGetValue(u.Id, out var roles) ? roles : Array.Empty<string>()))
            .ToList();
    }

    public async Task<bool> SetUserActiveAsync(string userId, bool isActive)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.IsActive = isActive;

        // تجديد الـ SecurityStamp يجعل جلسات المستخدم الحالية باطلة
        if (!isActive)
            user.SecurityStamp = Guid.NewGuid().ToString();

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> CountActiveAdminsAsync()
    {
        var adminRoleId = await _context.Roles
            .Where(r => r.Name == "Admin")
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        if (adminRoleId == null) return 0;

        return await _context.UserRoles
            .Where(ur => ur.RoleId == adminRoleId)
            .Join(_context.Users, ur => ur.UserId, u => u.Id, (ur, u) => u)
            .CountAsync(u => u.IsActive);
    }

    public async Task<bool> IsInRoleAsync(string userId, string roleName)
    {
        var roleId = await _context.Roles
            .Where(r => r.Name == roleName)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        if (roleId == null) return false;

        return await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
    }
}
