namespace Wex.Cards.Api.Transactions;

public sealed record GetConvertedTransactionResponse(
    Guid Id,
    Guid CardId,
    string Description,
    DateOnly TransactionDate,
    decimal OriginalAmount,
    decimal ExchangeRate,
    decimal ConvertedAmount);
