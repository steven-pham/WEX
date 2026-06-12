namespace Wex.Cards.Api.Transactions;

public sealed record AddTransactionRequest(string? Description, DateOnly? TransactionDate, decimal? Amount);
