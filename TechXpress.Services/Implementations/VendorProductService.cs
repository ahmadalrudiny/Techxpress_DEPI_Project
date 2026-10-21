using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;
using TechXpress.Data.Entities;
using TechXpress.Data.UoW;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;

namespace TechXpress.Services.Implementations;

public class VendorProductService : IVendorProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VendorProductService> _logger;
    private readonly string _imagesPath;

    public VendorProductService(IUnitOfWork unitOfWork, ILogger<VendorProductService> logger, string imagesPath)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _imagesPath = imagesPath;
    }

    public async Task<(IEnumerable<ProductDto> Items, int TotalCount)> GetVendorProductsPagedAsync(
        int vendorId, int page = 1, int pageSize = 12)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 12;
        if (pageSize > 100) pageSize = 100;

        var query = _unitOfWork.Products.Query()
            .Where(p => p.VendorId == vendorId)
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(p => new ProductDto(
            p.Id, p.Name, p.Description, p.Price, p.StockQuantity, p.ImageUrl,
            p.IsActive, p.CategoryId,
            p.Category?.Name ?? string.Empty,
            p.VendorId,
            p.Vendor?.StoreName,
            p.CreatedAt
        ));

        return (dtos, totalCount);
    }

    public async Task<ProductDto?> GetVendorProductByIdAsync(int productId, int vendorId)
    {
        var product = await _unitOfWork.Products.Query()
            .Include(p => p.Category)
            .Include(p => p.Vendor)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId && p.VendorId == vendorId);

        if (product == null) return null;

        return new ProductDto(
            product.Id, product.Name, product.Description, product.Price,
            product.StockQuantity, product.ImageUrl, product.IsActive,
            product.CategoryId, product.Category?.Name ?? string.Empty,
            product.VendorId, product.Vendor?.StoreName, product.CreatedAt
        );
    }

    public async Task<(bool Success, string Message, int? ProductId)> CreateProductAsync(
        int vendorId, ProductCreateRequest request)
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

        var vendor = await _unitOfWork.Vendors.GetByIdAsync(vendorId);
        if (vendor == null || vendor.Status != VendorStatus.Approved)
            return (false, "حساب المتجر غير موجود أو غير معتمد.", null);

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

        _logger.LogInformation("Product {ProductId} created for vendor {VendorId}", product.Id, vendorId);
        return (true, "تم إنشاء المنتج بنجاح.", product.Id);
    }

    public async Task<(bool Success, string Message)> UpdateProductAsync(
        int productId, int vendorId, ProductUpdateRequest request)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null)
            return (false, "المنتج غير موجود.");

        if (product.VendorId != vendorId)
            return (false, "لا تملك صلاحية تعديل هذا المنتج.");

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
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                var oldPath = Path.Combine(_imagesPath, Path.GetFileName(product.ImageUrl));
                if (File.Exists(oldPath))
                    File.Delete(oldPath);
            }
            product.ImageUrl = await SaveImageAsync(request.Image);
        }

        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Product {ProductId} updated for vendor {VendorId}", productId, vendorId);
        return (true, "تم تحديث المنتج بنجاح.");
    }

    public async Task<(bool Success, string Message)> DeactivateProductAsync(int productId, int vendorId)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null)
            return (false, "المنتج غير موجود.");

        if (product.VendorId != vendorId)
            return (false, "لا تملك صلاحية تعديل هذا المنتج.");

        product.IsActive = false;
        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Product {ProductId} deactivated for vendor {VendorId}", productId, vendorId);
        return (true, "تم تعطيل المنتج بنجاح.");
    }

    private async Task<string> SaveImageAsync(UploadedFile file)
    {
        if (!Directory.Exists(_imagesPath))
            Directory.CreateDirectory(_imagesPath);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        if (!allowedExtensions.Contains(extension))
            throw new InvalidOperationException("Invalid file type. Only JPG, JPEG, PNG, GIF, and WEBP are allowed.");

        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("File size exceeds 5 MB limit.");

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_imagesPath, fileName);

        using var fileStream = new FileStream(filePath, FileMode.Create);
        await file.Content.CopyToAsync(fileStream);

        return $"/images/products/{fileName}";
    }
}
