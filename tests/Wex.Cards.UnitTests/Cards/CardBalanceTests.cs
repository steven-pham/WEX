using NSubstitute;
using Wex.Cards.Application.Cards;
using Wex.Cards.Application.Ports;
using Wex.Cards.Domain.Entities;
using Wex.Cards.Domain.Exceptions;

namespace Wex.Cards.UnitTests.Cards;

public sealed class CardBalanceTests
{
    private static (CardService service, IExchangeRateProvider rateProvider) BuildService(
        Card? card,
        decimal totalSpent = 0m)
    {
        var cardRepo = Substitute.For<ICardRepository>();
        var txRepo = Substitute.For<ITransactionRepository>();
        var rateProvider = Substitute.For<IExchangeRateProvider>();

        if (card is not null)
        {
            cardRepo.GetByIdAsync(card.Id).Returns(card);
            txRepo.GetTotalSpentAsync(card.Id).Returns(totalSpent);
        }
        else
        {
            cardRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Card?)null);
        }

        return (new CardService(cardRepo, txRepo, rateProvider), rateProvider);
    }

    // ---- native balance (no currency) ----

    [Fact]
    public async Task GetBalanceAsync_NoTransactions_ReturnsCreditLimitAsBalance()
    {
        var card = Card.Create(1000m);
        var (service, _) = BuildService(card, 0m);

        var result = await service.GetBalanceAsync(card.Id, null);

        Assert.Equal(1000m, result.AvailableBalance);
        Assert.Equal(1.0m, result.ExchangeRate);
        Assert.Equal(1000m, result.ConvertedBalance);
    }

    [Fact]
    public async Task GetBalanceAsync_WithTransactions_ReturnsCreditLimitMinusTotal()
    {
        var card = Card.Create(1000m);
        var (service, _) = BuildService(card, 225.50m);

        var result = await service.GetBalanceAsync(card.Id, null);

        Assert.Equal(774.50m, result.AvailableBalance);
        Assert.Equal(1.0m, result.ExchangeRate);
        Assert.Equal(774.50m, result.ConvertedBalance);
    }

    // ---- converted balance ----

    [Fact]
    public async Task GetBalanceAsync_WithCurrency_ReturnsConvertedBalanceUsingLatestRate()
    {
        var card = Card.Create(500m);
        var (service, rateProvider) = BuildService(card, 100m);
        rateProvider.GetLatestRateAsync("EUR", Arg.Any<CancellationToken>())
            .Returns(new ExchangeRate(1.084m, new DateOnly(2024, 6, 1)));

        var result = await service.GetBalanceAsync(card.Id, "EUR");

        Assert.Equal(400m, result.AvailableBalance);
        Assert.Equal(1.084m, result.ExchangeRate);
        Assert.Equal(Math.Round(400m * 1.084m, 2, MidpointRounding.ToEven), result.ConvertedBalance);
        await rateProvider.Received(1).GetLatestRateAsync("EUR", Arg.Any<CancellationToken>());
    }

    // ---- USD short-circuit ----

    [Fact]
    public async Task GetBalanceAsync_UsdCurrency_ReturnsBalanceWithoutProviderCall()
    {
        var card = Card.Create(300m);
        var (service, rateProvider) = BuildService(card, 0m);

        var result = await service.GetBalanceAsync(card.Id, "USD");

        Assert.Equal(1.0m, result.ExchangeRate);
        Assert.Equal(result.AvailableBalance, result.ConvertedBalance);
        await rateProvider.DidNotReceiveWithAnyArgs()
            .GetLatestRateAsync(default!, default);
    }

    [Fact]
    public async Task GetBalanceAsync_EmptyCurrency_ReturnsNativeBalanceWithoutProviderCall()
    {
        var card = Card.Create(300m);
        var (service, rateProvider) = BuildService(card, 0m);

        var result = await service.GetBalanceAsync(card.Id, "   ");

        Assert.Equal(1.0m, result.ExchangeRate);
        Assert.Equal(result.AvailableBalance, result.ConvertedBalance);
        await rateProvider.DidNotReceiveWithAnyArgs()
            .GetLatestRateAsync(default!, default);
    }

    // ---- unknown card → 404 ----

    [Fact]
    public async Task GetBalanceAsync_UnknownCard_ThrowsCardNotFoundException()
    {
        var (service, _) = BuildService(null);

        await Assert.ThrowsAsync<CardNotFoundException>(
            () => service.GetBalanceAsync(Guid.NewGuid(), null));
    }

    // ---- unsupported currency → conversion exception ----

    [Fact]
    public async Task GetBalanceAsync_NoRateAvailable_ThrowsCardBalanceCurrencyConversionException()
    {
        var card = Card.Create(500m);
        var (service, rateProvider) = BuildService(card, 0m);
        rateProvider.GetLatestRateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ExchangeRate?)null);

        await Assert.ThrowsAsync<CardBalanceCurrencyConversionException>(
            () => service.GetBalanceAsync(card.Id, "XYZ"));
    }
}
