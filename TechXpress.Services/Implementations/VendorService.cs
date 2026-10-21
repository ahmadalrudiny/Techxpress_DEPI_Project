using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechXpress.Data.Entities;
using TechXpress.Data.UoW;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;

namespace TechXpress.Services.Implementations;

public class VendorService : IVendorService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IVendorDocumentStorage _storage;
    private readonly ILogger<VendorService> _logger;

    public VendorService(
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        IVendorDocumentStorage storage,
        ILogger<VendorService> logger)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _storage = storage;
        _logger = logger;
    }

    public async Task<VendorApplicationStatus?> GetApplicationStatusAsync(string userId)
    {
        var v = await _unitOfWork.Vendors.GetByUserIdAsync(userId);
        return v == null
            ? null
            : new VendorApplicationStatus(v.Id, v.StoreName, v.Status, v.RejectionReason, v.SubmittedAt);
    }

    public async Task<(bool Success, string Message)> ApplyAsync(string userId, VendorApplicationRequest request)
    {
        var storeName = request.StoreName?.Trim();
        if (string.IsNullOrWhiteSpace(storeName) || storeName.Length > 100)
            return (false, "اسم المتجر مطلوب (حتى 100 حرف).");

        var existing = await _unitOfWork.Vendors.GetByUserIdAsync(userId);
        if (existing != null && existing.Status != VendorStatus.Rejected)
            return (false, existing.Status == VendorStatus.Approved
                ? "حسابك معتمد بالفعل كمورد."
                : "لديك طلب قيد المراجعة بالفعل.");

        var saved = new List<string>();
        try
        {
            var nationalId = await SaveDocumentAsync(request.NationalId, "البطاقة الشخصية", saved);
            if (nationalId.Error != null) { Cleanup(saved); return (false, nationalId.Error); }

            var register = await SaveDocumentAsync(request.CommercialRegister, "السجل التجاري", saved);
            if (register.Error != null) { Cleanup(saved); return (false, register.Error); }

            var taxCard = await SaveDocumentAsync(request.TaxCard, "البطاقة الضريبية", saved);
            if (taxCard.Error != null) { Cleanup(saved); return (false, taxCard.Error); }

            string[] oldFiles = Array.Empty<string>();

            if (existing == null)
            {
                await _unitOfWork.Vendors.AddAsync(new Vendor
                {
                    UserId = userId,
                    StoreName = storeName,
                    NationalIdPath = nationalId.Name!,
                    CommercialRegisterPath = register.Name!,
                    TaxCardPath = taxCard.Name!
                });
            }
            else
            {
                // إعادة التقديم بعد الرفض
                oldFiles = new[] { existing.NationalIdPath, existing.CommercialRegisterPath, existing.TaxCardPath };

                existing.StoreName = storeName;
                existing.NationalIdPath = nationalId.Name!;
                existing.CommercialRegisterPath = register.Name!;
                existing.TaxCardPath = taxCard.Name!;
                existing.Status = VendorStatus.Pending;
                existing.RejectionReason = null;
                existing.SubmittedAt = DateTime.UtcNow;
                existing.ReviewedAt = null;
                existing.ReviewedByUserId = null;
            }

            await _unitOfWork.SaveChangesAsync();

            foreach (var old in oldFiles) _storage.Delete(old);

            _logger.LogInformation("Vendor application submitted by user {UserId}", userId);
            return (true, "تم إرسال طلبك بنجاح وهو قيد المراجعة.");
        }
        catch (Exception ex)
        {
            Cleanup(saved);   // مفيش ملفات يتيمة لو الحفظ في الداتابيز فشل
            _logger.LogError(ex, "Failed to submit vendor application for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IReadOnlyList<VendorRequestItem>> GetRequestsAsync(VendorStatus? status)
    {
        var vendors = status == null
            ? await _unitOfWork.Vendors.GetAllWithUsersAsync()
            : await _unitOfWork.Vendors.GetByStatusAsync(status.Value);

        return vendors
            .Select(v => new VendorRequestItem(
                v.Id, v.StoreName, v.User.FullName, v.User.Email ?? "",
                v.SubmittedAt, v.Status, v.RejectionReason))
            .ToList();
    }

    public async Task<(bool Success, string Message)> ApproveAsync(int vendorId, string adminUserId)
    {
        var vendor = await _unitOfWork.Vendors.GetByIdWithUserAsync(vendorId);
        if (vendor == null)
            return (false, "الطلب غير موجود.");
        if (vendor.Status != VendorStatus.Pending)
            return (false, "تمت مراجعة هذا الطلب سابقاً.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            vendor.Status = VendorStatus.Approved;
            vendor.RejectionReason = null;
            vendor.ReviewedAt = DateTime.UtcNow;
            vendor.ReviewedByUserId = adminUserId;
            await _unitOfWork.SaveChangesAsync();

            if (!await _userManager.IsInRoleAsync(vendor.User, "Vendor"))
            {
                var roleResult = await _userManager.AddToRoleAsync(vendor.User, "Vendor");
                if (!roleResult.Succeeded)
                {
                    await _unitOfWork.RollbackAsync();
                    _logger.LogError("Could not add Vendor role to user {UserId}: {Errors}",
                        vendor.UserId, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    return (false, "تعذّر منح صلاحية المورد.");
                }
            }

            await _unitOfWork.CommitAsync();
            _logger.LogInformation("Vendor {VendorId} approved by admin {AdminId}", vendorId, adminUserId);
            return (true, "تمت الموافقة على المورد.");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while approving vendor {VendorId}", vendorId);
            await _unitOfWork.RollbackAsync();
            return (false, "تمت معالجة الطلب من أدمن آخر، حدّث الصفحة.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Failed to approve vendor {VendorId}", vendorId);
            throw;
        }
    }

    public async Task<(bool Success, string Message)> RejectAsync(int vendorId, string adminUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return (false, "سبب الرفض مطلوب.");

        var vendor = await _unitOfWork.Vendors.GetByIdWithUserAsync(vendorId);
        if (vendor == null)
            return (false, "الطلب غير موجود.");
        if (vendor.Status != VendorStatus.Pending)
            return (false, "تمت مراجعة هذا الطلب سابقاً.");

        reason = reason.Trim();
        vendor.Status = VendorStatus.Rejected;
        vendor.RejectionReason = reason.Length > 500 ? reason[..500] : reason;
        vendor.ReviewedAt = DateTime.UtcNow;
        vendor.ReviewedByUserId = adminUserId;

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while rejecting vendor request {VendorId}", vendorId);
            return (false, "تمت معالجة الطلب من أدمن آخر، حدّث الصفحة.");
        }

        _logger.LogInformation("Vendor {VendorId} rejected by admin {AdminId}", vendorId, adminUserId);
        return (true, "تم رفض الطلب.");
    }

    public async Task<StoredDocument?> OpenDocumentAsync(int vendorId, VendorDocumentType type, string adminUserId)
    {
        var vendor = await _unitOfWork.Vendors.GetByIdAsync(vendorId);
        if (vendor == null) return null;

        var stored = type switch
        {
            VendorDocumentType.NationalId => vendor.NationalIdPath,
            VendorDocumentType.CommercialRegister => vendor.CommercialRegisterPath,
            VendorDocumentType.TaxCard => vendor.TaxCardPath,
            _ => null
        };
        if (stored == null) return null;

        _logger.LogInformation("Admin {AdminId} opened {Type} of vendor {VendorId}", adminUserId, type, vendorId);
        return _storage.Open(stored);
    }

    private async Task<(string? Name, string? Error)> SaveDocumentAsync(UploadedFile file, string label, List<string> saved)
    {
        var result = await _storage.SaveAsync(file);
        if (!result.Success) return (null, $"{label}: {result.Error}");

        saved.Add(result.StoredName!);
        return (result.StoredName, null);
    }

    private void Cleanup(IEnumerable<string> storedNames)
    {
        foreach (var name in storedNames) _storage.Delete(name);
    }

    public async Task<int?> GetVendorIdByUserIdAsync(string userId)
    {
        var vendor = await _unitOfWork.Vendors.GetByUserIdAsync(userId);
        return vendor?.Id;
    }

    public async Task<bool> IsApprovedAsync(int vendorId)
    {
        var vendor = await _unitOfWork.Vendors.GetByIdAsync(vendorId);
        return vendor != null && vendor.Status == VendorStatus.Approved;
    }

    public async Task<IEnumerable<OrderDetailDto>> GetVendorOrderDetailsAsync(int vendorId)
    {
        var orderDetails = await _unitOfWork.Orders.GetOrderDetailsByVendorIdAsync(vendorId);
        return orderDetails.Select(d => new OrderDetailDto(
            d.Id, d.ProductId, d.Product?.Name ?? string.Empty, d.Quantity,
            d.UnitPrice, d.VendorId, d.Vendor?.StoreName,
            d.ShippingStatus, d.Carrier, d.TrackingNumber
        ));
    }

    public async Task<IReadOnlyList<VendorOrderSummaryDto>> GetVendorOrdersAsync(int vendorId)
    {
        var orders = await _unitOfWork.Orders.GetOrdersByVendorIdAsync(vendorId);

        return orders.Select(o => new VendorOrderSummaryDto(
            o.Id,
            o.OrderDate,
            o.Status,
            o.OrderDetails.Sum(d => d.Quantity),
            o.OrderDetails.Sum(d => d.UnitPrice * d.Quantity),
            DeriveShippingStatus(o, vendorId)))
            .ToList();
    }

    public async Task<VendorOrderDetailsDto?> GetVendorOrderAsync(int vendorId, int orderId)
    {
        var order = await _unitOfWork.Orders.GetOrderForVendorAsync(orderId, vendorId);
        if (order == null) return null;

        var items = order.OrderDetails.Select(d => new VendorOrderItemDto(
            d.ProductId,
            d.Product?.Name ?? string.Empty,
            d.Product?.ImageUrl,
            d.Quantity,
            d.UnitPrice,
            d.UnitPrice * d.Quantity,
            d.ShippingStatus)).ToList();

        var shipped = order.OrderDetails.FirstOrDefault(d => d.ShippedAt != null);
        var deliveredAt = order.OrderDetails
            .Where(d => d.DeliveredAt != null)
            .Select(d => d.DeliveredAt)
            .Max();

        return new VendorOrderDetailsDto(
            order.Id,
            order.OrderDate,
            order.Status,
            order.ShippingAddress,
            items,
            DeriveShippingStatus(order, vendorId),
            shipped?.Carrier,
            shipped?.TrackingNumber,
            shipped?.ShippedAt,
            deliveredAt,
            order.User?.FullName,
            order.User?.PhoneNumber);
    }

    // ===== الشحن (مسؤولية البائع) =====

    public async Task<(bool Success, string Message)> StartPreparingAsync(int vendorId, int orderId)
    {
        var (order, items, error) = await LoadVendorShipmentAsync(vendorId, orderId);
        if (order == null) return (false, error!);

        if (order.Status == OrderStatus.Pending)
            return (false, "لا يمكن تجهيز طلب لم يتم دفعه بعد.");
        if (order.Status == OrderStatus.Cancelled)
            return (false, "لا يمكن تجهيز طلب ملغي.");
        if (items.All(d => d.ShippingStatus == ShippingStatus.Delivered))
            return (false, "تم توصيل هذا الطلب بالفعل.");
        if (items.All(d => d.ShippingStatus != ShippingStatus.Pending))
            return (false, "الطلب قيد التجهيز أو تم شحنه بالفعل.");

        foreach (var item in items.Where(d => d.ShippingStatus == ShippingStatus.Pending))
        {
            item.ShippingStatus = ShippingStatus.Preparing;
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Vendor {VendorId} started preparing order {OrderId}", vendorId, orderId);
        return (true, "تم بدء تجهيز الطلب.");
    }

    public async Task<(bool Success, string Message)> ShipOrderAsync(
        int vendorId, int orderId, string carrier, string? trackingNumber)
    {
        var carrierName = carrier?.Trim() ?? string.Empty;
        if (carrierName.Length < 2)
            return (false, "يرجى إدخال اسم شركة الشحن.");
        if (carrierName.Length > 100)
            return (false, "اسم شركة الشحن طويل جدًا (100 حرف كحد أقصى).");

        var tracking = string.IsNullOrWhiteSpace(trackingNumber) ? null : trackingNumber.Trim();
        if (tracking?.Length > 100)
            return (false, "رقم التتبع طويل جدًا (100 حرف كحد أقصى).");

        var (order, items, error) = await LoadVendorShipmentAsync(vendorId, orderId);
        if (order == null) return (false, error!);

        if (order.Status == OrderStatus.Pending)
            return (false, "لا يمكن شحن طلب لم يتم دفعه بعد.");
        if (order.Status == OrderStatus.Cancelled)
            return (false, "لا يمكن شحن طلب ملغي.");
        if (order.Status == OrderStatus.Delivered)
            return (false, "تم توصيل هذا الطلب بالفعل.");
        if (items.All(d => d.ShippingStatus is ShippingStatus.Shipped or ShippingStatus.Delivered))
            return (false, "تم شحن منتجاتك في هذا الطلب بالفعل.");

        var now = DateTime.UtcNow;
        foreach (var item in items.Where(d => d.ShippingStatus is ShippingStatus.Pending or ShippingStatus.Preparing))
        {
            item.ShippingStatus = ShippingStatus.Shipped;
            item.ShippedAt = now;
            item.Carrier = carrierName;
            item.TrackingNumber = tracking;
        }

        SyncOrderStatusWithShipment(order);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Vendor {VendorId} shipped order {OrderId} via {Carrier}", vendorId, orderId, carrierName);
        return (true, "تم شحن الطلب بنجاح، سيظهر للعميل رقم التتبع.");
    }

    public async Task<(bool Success, string Message)> MarkDeliveredAsync(int vendorId, int orderId)
    {
        var (order, items, error) = await LoadVendorShipmentAsync(vendorId, orderId);
        if (order == null) return (false, error!);

        if (order.Status == OrderStatus.Cancelled)
            return (false, "لا يمكن تعديل طلب ملغي.");
        if (items.All(d => d.ShippingStatus == ShippingStatus.Delivered))
            return (false, "تم توصيل هذا الطلب بالفعل.");
        if (items.Any(d => d.ShippingStatus is ShippingStatus.Pending or ShippingStatus.Preparing))
            return (false, "يجب شحن الطلب قبل تأكيد التوصيل.");

        var now = DateTime.UtcNow;
        foreach (var item in items.Where(d => d.ShippingStatus == ShippingStatus.Shipped))
        {
            item.ShippingStatus = ShippingStatus.Delivered;
            item.DeliveredAt = now;
        }

        SyncOrderStatusWithShipment(order);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Vendor {VendorId} delivered order {OrderId}", vendorId, orderId);
        return (true, "تم تأكيد توصيل الطلب.");
    }

    /// <summary>يحمل الطلب مع كل بنوده ثم يعزل بنود البائع الحالي ويتحقق من ملكيتها.</summary>
    private async Task<(Order? Order, List<OrderDetail> Items, string? Error)> LoadVendorShipmentAsync(
        int vendorId, int orderId)
    {
        var order = await _unitOfWork.Orders.GetByIdWithDetailsAsync(orderId);
        if (order == null)
            return (null, new List<OrderDetail>(), "الطلب غير موجود.");

        var items = order.OrderDetails.Where(d => d.VendorId == vendorId).ToList();
        if (items.Count == 0)
            return (null, new List<OrderDetail>(), "لا تملك منتجات في هذا الطلب.");

        return (order, items, null);
    }

    /// <summary>حالة الشحن الظاهرة للبائع محسوبة من بنوده فقط (مع دعم الطلبات القديمة).</summary>
    private static ShippingStatus DeriveShippingStatus(Order order, int vendorId)
    {
        var items = order.OrderDetails.Where(d => d.VendorId == vendorId).ToList();
        if (items.Count == 0) return ShippingStatus.Pending;

        if (items.All(d => d.ShippingStatus == ShippingStatus.Delivered))
            return ShippingStatus.Delivered;

        if (items.All(d => d.ShippingStatus is ShippingStatus.Shipped or ShippingStatus.Delivered)
            && items.Any(d => d.ShippingStatus == ShippingStatus.Shipped))
            return ShippingStatus.Shipped;

        if (items.Any(d => d.ShippingStatus == ShippingStatus.Preparing))
            return ShippingStatus.Preparing;

        // طلبات أُنشئت قبل إضافة تتبع الشحن: نعتمد على حالة الطلب العامة
        if (items.All(d => d.ShippingStatus == ShippingStatus.Pending))
        {
            return order.Status switch
            {
                OrderStatus.Delivered => ShippingStatus.Delivered,
                OrderStatus.Shipped => ShippingStatus.Shipped,
                _ => ShippingStatus.Pending
            };
        }

        return ShippingStatus.Pending;
    }

    /// <summary>
    /// يرفع حالة الطلب العامة تلقائيًا عندما تُشحن/تُسلَّم كل بنود الطلب من جميع البائعين.
    /// </summary>
    private static void SyncOrderStatusWithShipment(Order order)
    {
        if (order.Status is OrderStatus.Cancelled or OrderStatus.Delivered)
            return;

        var details = order.OrderDetails.ToList();
        if (details.Count == 0)
            return;

        if (details.All(d => d.ShippingStatus == ShippingStatus.Delivered))
        {
            order.Status = OrderStatus.Delivered;
            return;
        }

        if (order.Status == OrderStatus.Paid &&
            details.All(d => d.ShippingStatus is ShippingStatus.Shipped or ShippingStatus.Delivered))
        {
            order.Status = OrderStatus.Shipped;
        }
    }

    public async Task<VendorDashboardStats> GetDashboardStatsAsync(int vendorId)
    {
        var products = (await _unitOfWork.Products.GetByVendorIdAsync(vendorId)).ToList();
        var orders = (await _unitOfWork.Orders.GetOrdersByVendorIdAsync(vendorId)).ToList();
        var transactions = (await _unitOfWork.Wallets.GetTransactionsByVendorIdAsync(vendorId)).ToList();

        // إجمالي المبيعات يحتسب الطلبات المدفوعة فقط (مدفوع / تم الشحن / تم التوصيل).
        // الطلبات غير المدفوعة (قيد الانتظار) والملغاة لا تُحتسب كمبيعات.
        var salesOrders = orders
            .Where(o => o.Status is OrderStatus.Paid or OrderStatus.Shipped or OrderStatus.Delivered)
            .ToList();

        // عدادات الشحن تُحسب من بنود البائع نفسه وليس من الحالة العامة للطلب
        var activeOrders = orders.Where(o => o.Status != OrderStatus.Cancelled).ToList();
        var shippingStates = activeOrders.ToDictionary(o => o.Id, o => DeriveShippingStatus(o, vendorId));

        return new VendorDashboardStats(
            TotalProducts: products.Count,
            ActiveProducts: products.Count(p => p.IsActive),
            InactiveProducts: products.Count(p => !p.IsActive),
            TotalOrders: orders.Count,
            AwaitingShipment: activeOrders.Count(o => o.Status == OrderStatus.Paid
                && shippingStates[o.Id] is ShippingStatus.Pending or ShippingStatus.Preparing),
            ShippedOrders: shippingStates.Values.Count(s => s == ShippingStatus.Shipped),
            DeliveredOrders: shippingStates.Values.Count(s => s == ShippingStatus.Delivered),
            CancelledOrders: orders.Count(o => o.Status == OrderStatus.Cancelled),
            TotalSales: salesOrders.Sum(o => o.OrderDetails.Sum(d => d.UnitPrice * d.Quantity)),
            PendingPayouts: transactions.Count(t => t.Type == WalletTransactionType.Withdrawal
                                                 && t.Status == WalletTransactionStatus.Pending));
    }
}