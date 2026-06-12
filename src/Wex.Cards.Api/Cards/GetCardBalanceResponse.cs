namespace Wex.Cards.Api.Cards;

public sealed record GetCardBalanceResponse(
    decimal AvailableBalance,
    decimal ExchangeRate,
    decimal ConvertedBalance);
