using Wex.Cards.Domain.Entities;
using Wex.Cards.Domain.Exceptions;

namespace Wex.Cards.UnitTests.Transactions;

public sealed class TransactionTests
{
    private static readonly Guid SomeCardId = Guid.NewGuid();
    private static readonly DateOnly SomeDate = new(2024, 1, 15);

    [Fact]
    public void Create_WithValidInputs_ReturnsTransactionInUsd()
    {
        var transaction = Transaction.Create(SomeCardId, "Coffee", SomeDate, 5.00m);

        Assert.Equal(SomeCardId, transaction.CardId);
        Assert.Equal("Coffee", transaction.Description);
        Assert.Equal(SomeDate, transaction.TransactionDate);
        Assert.Equal(5.00m, transaction.Amount.Amount);
        Assert.Equal("USD", transaction.Amount.Currency);
    }

    [Fact]
    public void Create_AssignsNonEmptyGuid()
    {
        var transaction = Transaction.Create(SomeCardId, "Coffee", SomeDate, 5.00m);

        Assert.NotEqual(Guid.Empty, transaction.Id);
    }

    [Fact]
    public void Create_CalledTwice_AssignsDifferentIds()
    {
        var t1 = Transaction.Create(SomeCardId, "Coffee", SomeDate, 5.00m);
        var t2 = Transaction.Create(SomeCardId, "Coffee", SomeDate, 5.00m);

        Assert.NotEqual(t1.Id, t2.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Create_WithNonPositiveAmount_ThrowsTransactionDomainException(double amount)
    {
        Assert.Throws<TransactionDomainException>(() =>
            Transaction.Create(SomeCardId, "Coffee", SomeDate, (decimal)amount));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceDescription_ThrowsTransactionDomainException(string description)
    {
        Assert.Throws<TransactionDomainException>(() =>
            Transaction.Create(SomeCardId, description, SomeDate, 5.00m));
    }

    [Theory]
    [InlineData("5.00001")]
    [InlineData("10.12345")]
    [InlineData("0.000001")]
    public void Create_WithMoreThanFourDecimalPlaces_ThrowsTransactionDomainException(string input)
    {
        Assert.Throws<TransactionDomainException>(() =>
            Transaction.Create(SomeCardId, "Coffee", SomeDate, decimal.Parse(input)));
    }

    [Theory]
    [InlineData("5.0000")]
    [InlineData("10.1234")]
    public void Create_WithAtMostFourDecimalPlaces_Succeeds(string input)
    {
        var transaction = Transaction.Create(SomeCardId, "Coffee", SomeDate, decimal.Parse(input));

        Assert.Equal(decimal.Parse(input), transaction.Amount.Amount);
    }

    [Fact]
    public void Create_WithWhitespacePaddedDescription_TrimsDescription()
    {
        var transaction = Transaction.Create(SomeCardId, "  Coffee  ", SomeDate, 5.00m);

        Assert.Equal("Coffee", transaction.Description);
    }
}
