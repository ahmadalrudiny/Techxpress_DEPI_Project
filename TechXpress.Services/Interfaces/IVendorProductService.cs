using TechXpress.Services.Models;

namespace TechXpress.Services.Interfaces;

public interface IVendorProductService
{
    /// <summary>Gets paged products for a specific vendor.</summary>
    Task<(IEnumerable<ProductDto> Items, int TotalCount)> GetVendorProductsPagedAsync(
        int vendorId, int page = 1, int pageSize = 12);

    /// <summary>Gets a product by ID if owned by the vendor.</summary>
    Task<ProductDto?> GetVendorProductByIdAsync(int productId, int vendorId);

    /// <summary>Creates a new product for a vendor.</summary>
    Task<(bool Success, string Message, int? ProductId)> CreateProductAsync(
        int vendorId, ProductCreateRequest request);

    /// <summary>Updates a product if owned by the vendor.</summary>
    Task<(bool Success, string Message)> UpdateProductAsync(
        int productId, int vendorId, ProductUpdateRequest request);

    /// <summary>Deactivates (sets IsActive=false) a product if owned by the vendor.</summary>
    Task<(bool Success, string Message)> DeactivateProductAsync(int productId, int vendorId);
}
