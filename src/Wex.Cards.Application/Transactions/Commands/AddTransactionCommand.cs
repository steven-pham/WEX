namespace Wex.Cards.Application.Transactions.Commands;

public sealed record AddTransactionCommand(Guid CardId, string? Description, DateOnly? TransactionDate, decimal? Amount);
