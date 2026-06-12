using Wex.Cards.Domain.Exceptions;
using Wex.Cards.Domain.ValueObjects;

namespace Wex.Cards.Domain.Entities;

public sealed class Card
{
    private Card() { }

    public Guid Id { get; private set; }
    public Money CreditLimit { get; private set; } = null!;

    public static Card Create(decimal creditLimitAmount)
    {
        if (creditLimitAmount < 0)
            throw new CardDomainException("Credit limit cannot be negative.");

        if (creditLimitAmount != Math.Round(creditLimitAmount, 2, MidpointRounding.AwayFromZero))
            throw new CardDomainException("Credit limit must have at most 2 decimal places.");

        return new Card
        {
            Id = Guid.NewGuid(),
            CreditLimit = Money.Usd(creditLimitAmount)
        };
    }
}
