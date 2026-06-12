namespace Wex.Cards.Domain.ValueObjects;

public sealed record Money(decimal Amount, string Currency)
{
    public static Money Usd(decimal amount) => new(amount, CurrencyCode.Usd);
}
