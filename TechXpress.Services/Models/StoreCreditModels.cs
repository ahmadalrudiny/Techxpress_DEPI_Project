using TechXpress.Data.Entities;

namespace TechXpress.Services.Models;

/// <summary>حركة واحدة على رصيد نقاط العميل.</summary>
public record StoreCreditTransactionDto(
    int Id,
    decimal Amount,
    decimal BalanceAfter,
    StoreCreditTransactionType Type,
    int? OrderId,
    string Description,
    DateTime CreatedAt);

/// <summary>ملخص رصيد النقاط وحركاته لعرضه في صفحة العميل.</summary>
public record StoreCreditSummaryDto(
    decimal Balance,
    IReadOnlyList<StoreCreditTransactionDto> Transactions);
