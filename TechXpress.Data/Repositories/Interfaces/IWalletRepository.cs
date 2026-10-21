using TechXpress.Data.Entities;

namespace TechXpress.Data.Repositories.Interfaces;

public interface IWalletRepository : IGenericRepository<WalletTransaction>
{
    Task<IEnumerable<WalletTransaction>> GetTransactionsByVendorIdAsync(int vendorId);
    Task<IEnumerable<WalletTransaction>> GetPendingPayoutRequestsAsync();

    /// <summary>Gets withdrawal (payout) requests with their vendor, optionally filtered by status.</summary>
    Task<IEnumerable<WalletTransaction>> GetPayoutRequestsAsync(WalletTransactionStatus? status);

    /// <summary>Counts payout requests grouped by status (for the admin history tabs).</summary>
    Task<(int Total, int Pending, int Approved, int Rejected)> GetPayoutRequestCountsAsync();

    Task<WalletTransaction?> GetByIdWithVendorAsync(int id);
    Task<bool> HasDepositForOrderDetailAsync(int orderDetailId);
}