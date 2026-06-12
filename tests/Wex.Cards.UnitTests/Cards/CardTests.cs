using Wex.Cards.Domain.Entities;
using Wex.Cards.Domain.Exceptions;

namespace Wex.Cards.UnitTests.Cards;

public sealed class CardTests
{
    [Fact]
    public void Create_WithValidCreditLimit_ReturnsCreditLimitInUsd()
    {
        var card = Card.Create(1000.00m);

        Assert.Equal(1000.00m, card.CreditLimit.Amount);
        Assert.Equal("USD", card.CreditLimit.Currency);
    }

    [Fact]
    public void Create_AssignsNonEmptyGuid()
    {
        var card = Card.Create(500m);

        Assert.NotEqual(Guid.Empty, card.Id);
    }

    [Fact]
    public void Create_CalledTwice_AssignsDifferentIds()
    {
        var card1 = Card.Create(100m);
        var card2 = Card.Create(100m);

        Assert.NotEqual(card1.Id, card2.Id);
    }

    [Fact]
    public void Create_WithZeroCreditLimit_Succeeds()
    {
        var card = Card.Create(0m);

        Assert.Equal(0m, card.CreditLimit.Amount);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1000)]
    public void Create_WithNegativeCreditLimit_ThrowsCardDomainException(double creditLimit)
    {
        Assert.Throws<CardDomainException>(() => Card.Create((decimal)creditLimit));
    }

    [Theory]
    [InlineData("1000.123")]
    [InlineData("0.001")]
    [InlineData("99.999")]
    public void Create_WithMoreThanTwoDecimalPlaces_ThrowsCardDomainException(string input)
    {
        Assert.Throws<CardDomainException>(() => Card.Create(decimal.Parse(input)));
    }
}
