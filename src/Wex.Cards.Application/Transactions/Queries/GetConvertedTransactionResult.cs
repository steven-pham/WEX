namespace Wex.Cards.Application.Transactions.Queries;

public sealed record GetConvertedTransactionResult(
    Guid Id,
    Guid CardId,
    string Description,
    DateOnly TransactionDate,
    decimal OriginalAmount,
    decimal ExchangeRate,
    decimal ConvertedAmount);
