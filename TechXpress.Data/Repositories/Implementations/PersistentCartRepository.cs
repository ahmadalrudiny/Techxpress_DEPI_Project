using Microsoft.EntityFrameworkCore;
using TechXpress.Data.Context;
using TechXpress.Data.Repositories.Interfaces;

namespace TechXpress.Data.Repositories.Implementations;

public class PersistentCartRepository : IPersistentCartRepository
{
    private readonly ApplicationDbContext _context;

    public PersistentCartRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetCartJsonAsync(string userId)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.CartJson)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> SaveCartJsonAsync(string userId, string? cartJson)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return false;
        }

        user.CartJson = string.IsNullOrWhiteSpace(cartJson) ? null : cartJson;
        return true;
    }
}
