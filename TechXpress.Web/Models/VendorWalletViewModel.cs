using System.ComponentModel.DataAnnotations;

namespace TechXpress.Web.Models;

public class VendorWalletViewModel
{
    public string StoreName { get; set; } = null!;
    public decimal Balance { get; set; }
    public List<WalletTransactionItem> Transactions { get; set; } = new();
}

public class WalletTransactionItem
{
    public DateTime Date { get; set; }
    public string Type { get; set; } = null!;
    public string Status { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }

    /// <summary>سبب رفض طلب السحب (إن وُجد) — يظهر للبائع في كشف الحساب.</summary>
    public string? RejectionReason { get; set; }

    /// <summary>تاريخ مراجعة طلب السحب (اعتماد/رفض) إن وُجد.</summary>
    public DateTime? ProcessedAt { get; set; }
}

public class PayoutInputModel
{
    [Range(1, 1000000, ErrorMessage = "Amount must be between 1 and 1,000,000")]
    public decimal Amount { get; set; }

    [Required, StringLength(100)]
    public string PayoutDetails { get; set; } = null!;

    [StringLength(200)]
    public string? Note { get; set; }
}