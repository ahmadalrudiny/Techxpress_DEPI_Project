using TechXpress.Data.Entities;

namespace TechXpress.Services.Models;

public record VendorWalletSummary(
    int VendorId,
    string StoreName,
    decimal Balance,
    IReadOnlyList<WalletTransaction> Transactions);