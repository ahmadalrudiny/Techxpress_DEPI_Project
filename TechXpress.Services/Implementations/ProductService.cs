using Microsoft.EntityFrameworkCore;
using System.IO;
using TechXpress.Data.Entities;
using TechXpress.Data.UoW;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;
using Microsoft.Extensions.Logging;

namespace TechXpress.Services.Implementations;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IUnitOfWork unitOfWork, ILogger<ProductService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<(IEnumerable<ProductDto> Items, int TotalCount)> GetPagedAsync(
        string? search = null,
        int? categoryId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string sort = "newest",
        int page = 1,
        int pageSize = 12)
    {
        // تطبيع معاملات الترقيم
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 12;
        if (pageSize > 100) pageSize = 100;

        // استعلام للقراءة فقط مع تضمين الفئة والبائع
        var query = _unitOfWork.Products.Query()
            .Include(p => p.Category)
            .Include(p => p.Vendor)
            .AsNoTracking()
            .Where(p => p.IsActive);

        // البحث بالاسم
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Name.Contains(term));
        }

        // تصفية حسب الفئة
        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        // نطاق سعري
        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        // الترتيب
        query = sort switch
        {
            "priceAsc" => query.OrderBy(p => p.Price),
            "priceDesc" => query.OrderByDescending(p => p.Price),
            "nameAsc" => query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.CreatedAt) // newest افتراضياً
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(p => new ProductDto(
            p.Id,
            p.Name,
            p.Description,
            p.Price,
            p.StockQuantity,
            p.ImageUrl,
            p.IsActive,
            p.CategoryId,
            p.Category?.Name ?? string.Empty,
            p.VendorId,
            p.Vendor?.StoreName,
            p.CreatedAt
        ));

        _logger.LogInformation(
            "Retrieved {PageSize} products (page {Page}, total {TotalCount}) with search={Search}, category={CategoryId}",
            pageSize, page, totalCount, search ?? "none", categoryId.HasValue ? categoryId.Value.ToString() : "none");

        return (dtos, totalCount);
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var product = await _unitOfWork.Products.Query()
            .Include(p => p.Category)
            .Include(p => p.Vendor)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        if (product == null)
        {
            _logger.LogWarning("Product {ProductId} not found or not active", id);
            return null;
        }

        _logger.LogInformation("Retrieved product {ProductId}: {Name}", id, product.Name);

        return new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.StockQuantity,
            product.ImageUrl,
            product.IsActive,
            product.CategoryId,
            product.Category?.Name ?? string.Empty,
            product.VendorId,
            product.Vendor?.StoreName,
            product.CreatedAt
        );
    }

    public async Task<(IEnumerable<ProductDto> Items, int TotalCount)> GetAllProductsForAdminAsync(
        int page = 1, int pageSize = 12, string? search = null, int? categoryId = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 12;
        if (pageSize > 100) pageSize = 100;

        var query = _unitOfWork.Products.Query()
            .Include(p => p.Category)
            .Include(p => p.Vendor)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Name.Contains(term));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(p => new ProductDto(
            p.Id, p.Name, p.Description, p.Price, p.StockQuantity, p.ImageUrl,
            p.IsActive, p.CategoryId,
            p.Category?.Name ?? string.Empty,
            p.VendorId, p.Vendor?.StoreName, p.CreatedAt
        ));

        return (dtos, totalCount);
    }

    public async Task<(bool Success, string Message, int? ProductId)> CreateProductAsync(
        int? vendorId, ProductCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return (false, "اسم المنتج مطلوب.", null);
        if (request.Price < 0)
            return (false, "السعر يجب أن يكون أكبر من صفر.", null);
        if (request.StockQuantity < 0)
            return (false, "الكمية يجب أن تكون أكبر من صفر.", null);

        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId);
        if (category == null)
            return (false, "الفئة غير موجودة.", null);

        string? imageUrl = null;
        if (request.Image != null)
        {
            imageUrl = await SaveImageAsync(request.Image);
        }

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            ImageUrl = imageUrl,
            IsActive = true,
            CategoryId = request.CategoryId,
            VendorId = vendorId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Products.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Product {ProductId} created by admin", product.Id);
        return (true, "تم إنشاء المنتج بنجاح.", product.Id);
    }

    public async Task<(bool Success, string Message)> UpdateProductAsync(
        int id, ProductUpdateRequest request)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null)
            return (false, "المنتج غير موجود.");

        if (request.Price < 0)
            return (false, "السعر يجب أن يكون أكبر من صفر.");
        if (request.StockQuantity < 0)
            return (false, "الكمية يجب أن تكون أكبر من صفر.");

        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId);
        if (category == null)
            return (false, "الفئة غير موجودة.");

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.CategoryId = request.CategoryId;
        product.IsActive = request.IsActive;

        if (request.Image != null)
        {
            product.ImageUrl = await SaveImageAsync(request.Image);
        }

        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Product {ProductId} updated by admin", id);
        return (true, "تم تحديث المنتج بنجاح.");
    }

    public async Task<(bool Success, string Message)> DeactivateProductAsync(int id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null)
            return (false, "المنتج غير موجود.");

        product.IsActive = false;
        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Product {ProductId} deactivated by admin", id);
        return (true, "تم تعطيل المنتج بنجاح.");
    }

    public async Task<(bool Success, string Message)> RestoreProductAsync(int id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null)
            return (false, "المنتج غير موجود.");

        product.IsActive = true;
        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Product {ProductId} restored by admin", id);
        return (true, "تم استعادة المنتج بنجاح.");
    }

    private async Task<string> SaveImageAsync(UploadedFile file)
    {
        var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
        if (!Directory.Exists(imagesPath))
            Directory.CreateDirectory(imagesPath);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        if (!allowedExtensions.Contains(extension))
            throw new InvalidOperationException("Invalid file type.");

        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("File size exceeds 5 MB limit.");

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(imagesPath, fileName);

        using var fileStream = new FileStream(filePath, FileMode.Create);
        await file.Content.CopyToAsync(fileStream);

        return $"/images/products/{fileName}";
    }
}
