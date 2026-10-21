using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;
using TechXpress.Web.Models;

namespace TechXpress.Web.Controllers;

public class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IReviewService _reviewService;
    private readonly IWishlistService _wishlistService;

    public ProductsController(
        IProductService productService,
        ICategoryService categoryService,
        IReviewService reviewService,
        IWishlistService wishlistService)
    {
        _productService = productService;
        _categoryService = categoryService;
        _reviewService = reviewService;
        _wishlistService = wishlistService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        int? categoryId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string sort = "newest",
        int page = 1,
        int pageSize = 12)
    {
        var (products, totalCount) = await _productService.GetPagedAsync(
            search, categoryId, minPrice, maxPrice, sort, page, pageSize);

        var categories = await _categoryService.GetAllAsync();

        var model = new ProductListViewModel
        {
            Products = products,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            Search = search,
            CategoryId = categoryId,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Sort = sort,
            Categories = categories
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> SearchPartial(
        string? search = null,
        int? categoryId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string sort = "newest",
        int page = 1,
        int pageSize = 12)
    {
        var (products, totalCount) = await _productService.GetPagedAsync(
            search, categoryId, minPrice, maxPrice, sort, page, pageSize);

        // الفئات محتاجة عشان نتائج البحث الحي تتحدّث مع لوحة التصفية
        var model = new ProductListViewModel
        {
            Products = products,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            Search = search,
            CategoryId = categoryId,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Sort = sort
        };

        return PartialView("_ProductResultsPartial", model);
    }

    /// <summary>Live search suggestions (JSON) used by the navbar search box.</summary>
    [HttpGet]
    public async Task<IActionResult> Suggest(string? term = null, int take = 6)
    {
        term = term?.Trim();
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            return Json(Array.Empty<object>());

        if (take < 1) take = 1;
        if (take > 10) take = 10;

        var (products, _) = await _productService.GetPagedAsync(term, null, pageSize: take);

        var results = products.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            price = p.Price,
            image = p.ImageUrl,
            category = p.CategoryName,
            store = p.VendorStoreName,
            inStock = p.StockQuantity > 0
        });

        return Json(results);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null)
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var reviews = await _reviewService.GetProductReviewsAsync(id, userId);

        // وصف مخصص لمحركات البحث لكل منتج
        var seller = string.IsNullOrWhiteSpace(product.VendorStoreName) ? "TechXpress" : product.VendorStoreName;
        ViewData["Description"] =
            $"{product.Name} من {product.CategoryName} بسعر ${product.Price:F2} لدى {seller} — " +
            "اشترِ الآن بأمان عبر TechXpress مع دفع إلكتروني وتقييمات من مشترين حقيقيين.";

        var model = new ProductDetailsViewModel
        {
            Product = product,
            Reviews = reviews,
            InWishlist = userId != null && await _wishlistService.IsInWishlistAsync(userId, id)
        };

        return View(model);
    }
}
