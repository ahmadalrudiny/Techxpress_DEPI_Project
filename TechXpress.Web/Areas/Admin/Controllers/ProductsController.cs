using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechXpress.Services.Interfaces;
using TechXpress.Web.Areas.Admin.Models;

namespace TechXpress.Web.Areas.Admin.Controllers;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;

    public ProductsController(IProductService productService, ICategoryService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 12, string? search = null, int? categoryId = null)
    {
        var (products, totalCount) = await _productService.GetAllProductsForAdminAsync(page, pageSize, search, categoryId);
        var categories = await _categoryService.GetAllAsync();

        var model = new AdminProductListViewModel
        {
            Products = products,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            Search = search,
            CategoryId = categoryId,
            Categories = categories
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var categories = await _categoryService.GetAllAsync();
        var model = new ProductCreateViewModel
        {
            Categories = categories
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await _categoryService.GetAllAsync();
            return View(model);
        }

        var request = new TechXpress.Services.Models.ProductCreateRequest(
            model.Name,
            model.Description,
            model.Price,
            model.StockQuantity,
            model.CategoryId,
            model.Image != null ? new TechXpress.Services.Models.UploadedFile(
                model.Image.OpenReadStream(),
                model.Image.FileName,
                model.Image.Length) : null);

        var (success, message, productId) = await _productService.CreateProductAsync(
            null, request); // vendorId = null for admin-created products

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            model.Categories = await _categoryService.GetAllAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = "Product created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null) return NotFound();

        var model = new ProductEditViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            IsActive = product.IsActive,
            CurrentImageUrl = product.ImageUrl,
            Categories = await _categoryService.GetAllAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await _categoryService.GetAllAsync();
            return View(model);
        }

        var request = new TechXpress.Services.Models.ProductUpdateRequest(
            model.Name,
            model.Description,
            model.Price,
            model.StockQuantity,
            model.CategoryId,
            model.IsActive,
            model.Image != null ? new TechXpress.Services.Models.UploadedFile(
                model.Image.OpenReadStream(),
                model.Image.FileName,
                model.Image.Length) : null);

        var (success, message) = await _productService.UpdateProductAsync(id, request);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            model.Categories = await _categoryService.GetAllAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = "Product updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var (success, message) = await _productService.DeactivateProductAsync(id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id)
    {
        var (success, message) = await _productService.RestoreProductAsync(id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Index));
    }
}
