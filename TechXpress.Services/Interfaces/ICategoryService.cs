using TechXpress.Services.Models;

namespace TechXpress.Services.Interfaces;

public interface ICategoryService
{
    /// <summary>Gets all categories ordered by name.</summary>
    Task<IEnumerable<CategoryDto>> GetAllAsync();

    /// <summary>Gets a category by ID, or null if not found.</summary>
    Task<CategoryDto?> GetByIdAsync(int id);

    /// <summary>Creates a category (admin).</summary>
    Task<(bool Success, string Message, int? CategoryId)> CreateCategoryAsync(
        string name, string? description);

    /// <summary>Updates a category (admin).</summary>
    Task<(bool Success, string Message)> UpdateCategoryAsync(
        int id, string name, string? description);

    /// <summary>Deletes a category (admin). Blocked if it has products.</summary>
    Task<(bool Success, string Message)> DeleteCategoryAsync(int id);
}
