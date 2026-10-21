using System.ComponentModel.DataAnnotations;

namespace TechXpress.Data.Entities;

public class Vendor
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

    public string StoreName { get; set; } = null!;

    // مسارات الملفات (مخزنة خارج wwwroot)
    public string NationalIdPath { get; set; } = null!;
    public string CommercialRegisterPath { get; set; } = null!;
    public string TaxCardPath { get; set; } = null!;

    public VendorStatus Status { get; set; } = VendorStatus.Pending;
    public string? RejectionReason { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedByUserId { get; set; }

    public decimal WalletBalance { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }   // يمنع ضياع تحديث الرصيد

    public ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public enum VendorStatus
{
    Pending,
    Approved,
    Rejected
}