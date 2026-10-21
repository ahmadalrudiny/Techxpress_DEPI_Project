using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechXpress.Services.Interfaces;
using TechXpress.Web.Models;

namespace TechXpress.Web.Controllers;

[Authorize(Roles = "Vendor")]
public class VendorProductController : Controller
{
    private readonly IVendorProductService _vendorProductService;
    private readonly IVendorService _vendorService;
    private readonly ICategoryService _categoryService;

    public VendorProductController(
        IVendorProductService vendorProductService,
        IVendorService vendorService,
        ICategoryService categoryService)
    {
        _vendorProductService = vendorProductService;
        _vendorService = vendorService;
        _categoryService = categoryService;
    }

    private async Task<int?> GetCurrentVendorIdAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return null;
        return await _vendorService.GetVendorIdByUserIdAsync(userId);
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 12)
    {
        var vendorId = await GetCurrentVendorIdAsync();
        if (vendorId == null) return Forbid();

        var (products, totalCount) = await _vendorProductService.GetVendorProductsPagedAsync(
            vendorId.Value, page, pageSize);

        var model = new VendorProductListViewModel
        {
            Products = products,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vendorId = await GetCurrentVendorIdAsync();
        if (vendorId == null) return Forbid();

        if (!await _vendorService.IsApprovedAsync(vendorId.Value))
            return Forbid();

        var model = new ProductCreateViewModel
        {
            Categories = await _categoryService.GetAllAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductCreateViewModel model)
    {
        var vendorId = await GetCurrentVendorIdAsync();
        if (vendorId == null) return Forbid();

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

        var (success, message, productId) = await _vendorProductService.CreateProductAsync(
            vendorId.Value, request);

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
        var vendorId = await GetCurrentVendorIdAsync();
        if (vendorId == null) return Forbid();

        var product = await _vendorProductService.GetVendorProductByIdAsync(id, vendorId.Value);
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
        var vendorId = await GetCurrentVendorIdAsync();
        if (vendorId == null) return Forbid();

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

        var (success, message) = await _vendorProductService.UpdateProductAsync(
            id, vendorId.Value, request);

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
        var vendorId = await GetCurrentVendorIdAsync();
        if (vendorId == null) return Forbid();

        var (success, message) = await _vendorProductService.DeactivateProductAsync(id, vendorId.Value);

        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Index));
    }
}
