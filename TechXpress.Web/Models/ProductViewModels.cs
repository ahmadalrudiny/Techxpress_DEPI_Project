using TechXpress.Services.Models;

namespace TechXpress.Web.Models;

public class ProductListViewModel
{
    public IEnumerable<ProductDto> Products { get; set; } = new List<ProductDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string Sort { get; set; } = "newest";
    public IEnumerable<CategoryDto> Categories { get; set; } = new List<CategoryDto>();

    /// <summary>Arabic label for the current sort option (for the UI).</summary>
    public string SortLabel => Sort switch
    {
        "priceAsc" => "السعر: من الأقل للأعلى",
        "priceDesc" => "السعر: من الأعلى للأقل",
        "nameAsc" => "الاسم: أبجدياً",
        _ => "الأحدث أولاً"
    };
}

public class ProductDetailsViewModel
{
    public ProductDto Product { get; set; } = null!;
    public ProductReviewsDto Reviews { get; set; } = new(0, 0, new List<ReviewDto>(), false, false);
    public bool InWishlist { get; set; }
}

public class HomeIndexViewModel
{
    public IEnumerable<ProductDto> FeaturedProducts { get; set; } = new List<ProductDto>();
    public IEnumerable<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    public int TotalProducts { get; set; }
}
