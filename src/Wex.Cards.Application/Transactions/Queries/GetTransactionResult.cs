namespace Wex.Cards.Application.Transactions.Queries;

public sealed record GetTransactionResult(Guid Id, Guid CardId, string Description, DateOnly TransactionDate, decimal Amount);
