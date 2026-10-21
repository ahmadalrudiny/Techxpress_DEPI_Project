using TechXpress.Services.Models;

namespace TechXpress.Services.Interfaces;

public interface IProductService
{
    /// <summary>
    /// Gets a paged list of active products with optional search, category filter,
    /// price range, and sorting. Includes category name and vendor store name.
    /// </summary>
    /// <param name="search">Search term matched against product name.</param>
    /// <param name="categoryId">Filter by category ID.</param>
    /// <param name="minPrice">Minimum price filter.</param>
    /// <param name="maxPrice">Maximum price filter.</param>
    /// <param name="sort">Sorting: "newest" (default), "priceAsc", "priceDesc", "nameAsc".</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page (max 100).</param>
    Task<(IEnumerable<ProductDto> Items, int TotalCount)> GetPagedAsync(
        string? search = null,
        int? categoryId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string sort = "newest",
        int page = 1,
        int pageSize = 12);

    /// <summary>Gets a single active product by ID, or null if not found.</summary>
    Task<ProductDto?> GetByIdAsync(int id);

    /// <summary>Gets all products (including inactive) for admin with paging.</summary>
    Task<(IEnumerable<ProductDto> Items, int TotalCount)> GetAllProductsForAdminAsync(
        int page = 1, int pageSize = 12, string? search = null, int? categoryId = null);

    /// <summary>Creates a product (admin).</summary>
    Task<(bool Success, string Message, int? ProductId)> CreateProductAsync(
        int? vendorId, ProductCreateRequest request);

    /// <summary>Updates a product (admin). Sets VendorId unchanged.</summary>
    Task<(bool Success, string Message)> UpdateProductAsync(
        int id, ProductUpdateRequest request);

    /// <summary>Deactivates (sets IsActive=false) a product (admin).</summary>
    Task<(bool Success, string Message)> DeactivateProductAsync(int id);

    /// <summary>Restores (sets IsActive=true) a previously deactivated product (admin).</summary>
    Task<(bool Success, string Message)> RestoreProductAsync(int id);
}
