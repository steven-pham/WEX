namespace Wex.Cards.Application.Transactions.Commands;

public sealed record AddTransactionResult(Guid Id, Guid CardId, string Description, DateOnly TransactionDate, decimal Amount);
