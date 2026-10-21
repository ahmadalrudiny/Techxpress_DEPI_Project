namespace TechXpress.Data.Entities;

public class WalletTransaction
{
    public int Id { get; set; }

    public int VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    public int? OrderDetailId { get; set; }
    public OrderDetail? OrderDetail { get; set; }

    public decimal Amount { get; set; }          // موجب دايماً، والنوع بيحدد الاتجاه
    public decimal BalanceAfter { get; set; }

    public WalletTransactionType Type { get; set; }
    public WalletTransactionStatus Status { get; set; } = WalletTransactionStatus.Completed;

    public string? Description { get; set; }
    public string? PayoutDetails { get; set; }   // رقم المحفظة (للسحب فقط)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ===== بيانات مراجعة طلب السحب (سجل تدقيق) =====
    public DateTime? ProcessedAtUtc { get; set; }     // وقت قرار الأدمن
    public string? ProcessedByUserId { get; set; }    // معرّف الأدمن الذي اتخذ القرار
    public string? ProcessedByName { get; set; }      // اسم/بريد الأدمن وقت القرار
    public string? RejectionReason { get; set; }      // سبب الرفض (عند الرفض فقط)
}

public enum WalletTransactionType
{
    Deposit,
    Withdrawal,
    Refund
}

public enum WalletTransactionStatus
{
    Pending,
    Completed,
    Rejected
}