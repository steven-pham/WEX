namespace Wex.Cards.Api.Transactions;

public sealed record AddTransactionResponse(Guid Id, Guid CardId, string Description, DateOnly TransactionDate, decimal Amount);
