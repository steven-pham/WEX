namespace Wex.Cards.Application.Cards.Queries;

public sealed record GetCardBalanceResult(
    decimal AvailableBalance,
    decimal ExchangeRate,
    decimal ConvertedBalance);
