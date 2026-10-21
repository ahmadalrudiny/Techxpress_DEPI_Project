using TechXpress.Data.Entities;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;
using TechXpress.Data.UoW;
using Microsoft.Extensions.Logging;

namespace TechXpress.Services.Implementations;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(IUnitOfWork unitOfWork, ILogger<CategoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        var categories = await _unitOfWork.Categories.GetAllAsync();
        _logger.LogInformation("Retrieved {Count} categories", categories.Count());
        return categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Description));
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id);
        if (category == null)
        {
            _logger.LogWarning("Category {CategoryId} not found", id);
            return null;
        }

        _logger.LogInformation("Retrieved category {CategoryId}: {Name}", id, category.Name);
        return new CategoryDto(category.Id, category.Name, category.Description);
    }

    public async Task<(bool Success, string Message, int? CategoryId)> CreateCategoryAsync(
        string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "اسم الفئة مطلوب.", null);

        var category = new Category
        {
            Name = name.Trim(),
            Description = description
        };

        await _unitOfWork.Categories.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Category {CategoryId} created", category.Id);
        return (true, "تم إنشاء الفئة بنجاح.", category.Id);
    }

    public async Task<(bool Success, string Message)> UpdateCategoryAsync(
        int id, string name, string? description)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id);
        if (category == null)
            return (false, "الفئة غير موجودة.");

        category.Name = name.Trim();
        category.Description = description;

        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Category {CategoryId} updated", id);
        return (true, "تم تحديث الفئة بنجاح.");
    }

    public async Task<(bool Success, string Message)> DeleteCategoryAsync(int id)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id);
        if (category == null)
            return (false, "الفئة غير موجودة.");

        var products = await _unitOfWork.Products.GetByCategoryAsync(id);
        if (products.Any())
            return (false, "لا يمكن حذف فئة تحتوي على منتجات.");

        _unitOfWork.Categories.Delete(category);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Category {CategoryId} deleted", id);
        return (true, "تم حذف الفئة بنجاح.");
    }
}
