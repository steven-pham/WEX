using FluentValidation;
using Wex.Cards.Application.Cards.Commands;

namespace Wex.Cards.UnitTests.Cards;

public sealed class CreateCardCommandValidatorTests
{
    private readonly IValidator<CreateCardCommand> _sut = new CreateCardCommandValidator();

    [Fact]
    public void Validate_NullCreditLimit_IsRequired()
    {
        var result = _sut.Validate(new CreateCardCommand(null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCardCommand.CreditLimit));
    }

    [Fact]
    public void Validate_NegativeCreditLimit_IsRejected()
    {
        var result = _sut.Validate(new CreateCardCommand(-0.01m));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCardCommand.CreditLimit));
    }

    [Theory]
    [InlineData("1000.123")]
    [InlineData("0.001")]
    [InlineData("99.999")]
    public void Validate_MoreThanTwoDecimalPlaces_IsRejected(string input)
    {
        var result = _sut.Validate(new CreateCardCommand(decimal.Parse(input)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCardCommand.CreditLimit));
    }

    [Theory]
    [InlineData("0.00")]
    [InlineData("0")]
    [InlineData("1000.00")]
    [InlineData("1000.10")]
    [InlineData("999.99")]
    [InlineData("1000.1")]
    public void Validate_ValidCreditLimit_Passes(string input)
    {
        var result = _sut.Validate(new CreateCardCommand(decimal.Parse(input)));

        Assert.True(result.IsValid);
    }
}
