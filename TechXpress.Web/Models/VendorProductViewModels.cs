using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using TechXpress.Services.Models;

namespace TechXpress.Web.Models;

public class VendorProductListViewModel
{
    public IEnumerable<ProductDto> Products { get; set; } = new List<ProductDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class ProductCreateViewModel
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public IEnumerable<CategoryDto> Categories { get; set; } = new List<CategoryDto>();

    public IFormFile? Image { get; set; }
}

public class ProductEditViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public bool IsActive { get; set; }

    public string? CurrentImageUrl { get; set; }

    public IFormFile? Image { get; set; }

    public IEnumerable<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
}
