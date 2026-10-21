using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechXpress.Services.Interfaces;

using TechXpress.Web.Areas.Admin.Models;

namespace TechXpress.Web.Areas.Admin.Controllers;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class CategoriesController : Controller
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var categories = await _categoryService.GetAllAsync();
        return View(categories);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(string.Empty, "اسم الفئة مطلوب.");
            return View();
        }

        var (success, message, categoryId) = await _categoryService.CreateCategoryAsync(name, description);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return View();
        }

        TempData["SuccessMessage"] = "تم إنشاء الفئة بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        if (category == null) return NotFound();

        var model = new CategoryEditViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, message) = await _categoryService.UpdateCategoryAsync(
            id, model.Name, model.Description);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        TempData["SuccessMessage"] = "تم تحديث الفئة بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, message) = await _categoryService.DeleteCategoryAsync(id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Index));
    }
}
