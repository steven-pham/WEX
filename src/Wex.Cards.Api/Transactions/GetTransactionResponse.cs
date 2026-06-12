namespace Wex.Cards.Api.Transactions;

public sealed record GetTransactionResponse(Guid Id, Guid CardId, string Description, DateOnly TransactionDate, decimal Amount);
