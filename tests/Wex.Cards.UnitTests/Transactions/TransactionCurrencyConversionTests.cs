using NSubstitute;
using Wex.Cards.Application.Ports;
using Wex.Cards.Application.Transactions;
using Wex.Cards.Domain.Entities;
using Wex.Cards.Domain.Exceptions;

namespace Wex.Cards.UnitTests.Transactions;

public sealed class TransactionCurrencyConversionTests
{
    private static TransactionService BuildService(
        ITransactionRepository txRepo,
        IExchangeRateProvider rateProvider)
    {
        var cardRepo = Substitute.For<ICardRepository>();
        return new TransactionService(cardRepo, txRepo, rateProvider);
    }

    // ---- happy-path: conversion + rounding ----

    [Fact]
    public async Task GetAsync_ValidRate_ReturnsConvertedResult()
    {
        var cardId = Guid.NewGuid();
        var txDate = new DateOnly(2024, 6, 15);
        var transaction = Transaction.Create(cardId, "Fuel", txDate, 100.00m);
        var txRepo = Substitute.For<ITransactionRepository>();
        var rateProvider = Substitute.For<IExchangeRateProvider>();
        txRepo.GetByIdAsync(transaction.Id).Returns(transaction);
        rateProvider.GetRateOnOrBeforeAsync("EUR", txDate, 6, Arg.Any<CancellationToken>())
            .Returns(new ExchangeRate(1.084m, new DateOnly(2024, 6, 1)));
        var service = BuildService(txRepo, rateProvider);

        var result = await service.GetAsync(transaction.Id, "EUR");

        Assert.Equal(transaction.Id, result.Id);
        Assert.Equal(cardId, result.CardId);
        Assert.Equal("Fuel", result.Description);
        Assert.Equal(txDate, result.TransactionDate);
        Assert.Equal(100.00m, result.OriginalAmount);
        Assert.Equal(1.084m, result.ExchangeRate);
        Assert.Equal(108.40m, result.ConvertedAmount);
    }

    [Fact]
    public async Task GetAsync_ToEvenRounding_RoundsHalfToEven_Down()
    {
        // 10.00 * 1.1245 = 11.245 — midpoint; 4 is even so rounds DOWN to 11.24
        var txDate = new DateOnly(2024, 6, 15);
        var transaction = Transaction.Create(Guid.NewGuid(), "Shop", txDate, 10.00m);
        var txRepo = Substitute.For<ITransactionRepository>();
        var rateProvider = Substitute.For<IExchangeRateProvider>();
        txRepo.GetByIdAsync(transaction.Id).Returns(transaction);
        rateProvider.GetRateOnOrBeforeAsync(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new ExchangeRate(1.1245m, new DateOnly(2024, 6, 1)));
        var service = BuildService(txRepo, rateProvider);

        var result = await service.GetAsync(transaction.Id, "CAD");

        Assert.Equal(11.24m, result.ConvertedAmount);
    }

    [Fact]
    public async Task GetAsync_ToEvenRounding_RoundsHalfToEven_Up()
    {
        // 10.00 * 1.1255 = 11.255 — midpoint; 6 is even so rounds UP to 11.26
        var txDate = new DateOnly(2024, 6, 15);
        var transaction = Transaction.Create(Guid.NewGuid(), "Shop", txDate, 10.00m);
        var txRepo = Substitute.For<ITransactionRepository>();
        var rateProvider = Substitute.For<IExchangeRateProvider>();
        txRepo.GetByIdAsync(transaction.Id).Returns(transaction);
        rateProvider.GetRateOnOrBeforeAsync(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new ExchangeRate(1.1255m, new DateOnly(2024, 6, 1)));
        var service = BuildService(txRepo, rateProvider);

        var result = await service.GetAsync(transaction.Id, "CAD");

        Assert.Equal(11.26m, result.ConvertedAmount);
    }

    // ---- USD short-circuit: rate=1.0, converted==original ----

    [Fact]
    public async Task GetAsync_UsdCurrency_ConvertedEqualsOriginalWithoutProviderCall()
    {
        var txDate = new DateOnly(2024, 3, 15);
        var transaction = Transaction.Create(Guid.NewGuid(), "Coffee", txDate, 42.50m);
        var txRepo = Substitute.For<ITransactionRepository>();
        var rateProvider = Substitute.For<IExchangeRateProvider>();
        txRepo.GetByIdAsync(transaction.Id).Returns(transaction);
        var service = BuildService(txRepo, rateProvider);

        var result = await service.GetAsync(transaction.Id, "USD");

        Assert.Equal(1.0m, result.ExchangeRate);
        Assert.Equal(result.OriginalAmount, result.ConvertedAmount);
        await rateProvider.DidNotReceiveWithAnyArgs()
            .GetRateOnOrBeforeAsync(default!, default, default, default);
    }

    // ---- no qualifying rate → exception ----

    [Fact]
    public async Task GetAsync_NoQualifyingRate_ThrowsConversionException()
    {
        var txDate = new DateOnly(2024, 6, 15);
        var transaction = Transaction.Create(Guid.NewGuid(), "Coffee", txDate, 5.00m);
        var txRepo = Substitute.For<ITransactionRepository>();
        var rateProvider = Substitute.For<IExchangeRateProvider>();
        txRepo.GetByIdAsync(transaction.Id).Returns(transaction);
        rateProvider.GetRateOnOrBeforeAsync(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((ExchangeRate?)null);
        var service = BuildService(txRepo, rateProvider);

        await Assert.ThrowsAsync<TransactionCurrencyConversionException>(
            () => service.GetAsync(transaction.Id, "GBP"));
    }

    // ---- unknown transaction → 404 exception ----

    [Fact]
    public async Task GetAsync_UnknownTransaction_ThrowsTransactionNotFoundException()
    {
        var txRepo = Substitute.For<ITransactionRepository>();
        var rateProvider = Substitute.For<IExchangeRateProvider>();
        txRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Transaction?)null);
        var service = BuildService(txRepo, rateProvider);

        await Assert.ThrowsAsync<TransactionNotFoundException>(
            () => service.GetAsync(Guid.NewGuid(), "EUR"));
    }

    // ---- 6-month boundary: service passes transaction date + months=6 to provider ----

    [Fact]
    public async Task GetAsync_PassesTransactionDateAndSixMonthsToProvider()
    {
        var txDate = new DateOnly(2024, 12, 31);
        var transaction = Transaction.Create(Guid.NewGuid(), "Year-end", txDate, 200.00m);
        var txRepo = Substitute.For<ITransactionRepository>();
        var rateProvider = Substitute.For<IExchangeRateProvider>();
        txRepo.GetByIdAsync(transaction.Id).Returns(transaction);
        rateProvider.GetRateOnOrBeforeAsync("EUR", txDate, 6, Arg.Any<CancellationToken>())
            .Returns(new ExchangeRate(1.10m, new DateOnly(2024, 12, 28)));
        var service = BuildService(txRepo, rateProvider);

        await service.GetAsync(transaction.Id, "EUR");

        await rateProvider.Received(1)
            .GetRateOnOrBeforeAsync("EUR", txDate, 6, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_RateOnBoundaryCutoff_IncludedAndUsed()
    {
        // Cutoff = txDate.AddMonths(-6); rate exactly on cutoff qualifies (provider responsibility),
        // but service must pass date correctly so the provider can apply the inclusive boundary.
        var txDate = new DateOnly(2024, 6, 30);
        var cutoff = txDate.AddMonths(-6); // 2023-12-30
        var transaction = Transaction.Create(Guid.NewGuid(), "Boundary", txDate, 50.00m);
        var txRepo = Substitute.For<ITransactionRepository>();
        var rateProvider = Substitute.For<IExchangeRateProvider>();
        txRepo.GetByIdAsync(transaction.Id).Returns(transaction);
        rateProvider.GetRateOnOrBeforeAsync("EUR", txDate, 6, Arg.Any<CancellationToken>())
            .Returns(new ExchangeRate(0.92m, cutoff));
        var service = BuildService(txRepo, rateProvider);

        var result = await service.GetAsync(transaction.Id, "EUR");

        Assert.Equal(0.92m, result.ExchangeRate);
        Assert.Equal(Math.Round(50.00m * 0.92m, 2, MidpointRounding.ToEven), result.ConvertedAmount);
    }

    [Fact]
    public async Task GetAsync_NoRateOutsideWindow_ThrowsConversionException()
    {
        // Provider returns null when all rates are outside the 6-month window — service must throw.
        var txDate = new DateOnly(2024, 6, 30);
        var transaction = Transaction.Create(Guid.NewGuid(), "Old", txDate, 100.00m);
        var txRepo = Substitute.For<ITransactionRepository>();
        var rateProvider = Substitute.For<IExchangeRateProvider>();
        txRepo.GetByIdAsync(transaction.Id).Returns(transaction);
        rateProvider.GetRateOnOrBeforeAsync(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((ExchangeRate?)null);
        var service = BuildService(txRepo, rateProvider);

        await Assert.ThrowsAsync<TransactionCurrencyConversionException>(
            () => service.GetAsync(transaction.Id, "EUR"));
    }

    [Fact]
    public async Task GetAsync_MostRecentQualifyingRate_UsedInConversion()
    {
        // Provider is expected to return the most recent rate; service uses whatever it gets.
        // This test verifies the service correctly hands the result's Rate to the math.
        var txDate = new DateOnly(2024, 9, 30);
        var transaction = Transaction.Create(Guid.NewGuid(), "Multi-rate", txDate, 200.00m);
        var txRepo = Substitute.For<ITransactionRepository>();
        var rateProvider = Substitute.For<IExchangeRateProvider>();
        txRepo.GetByIdAsync(transaction.Id).Returns(transaction);
        // Provider returns the most recent qualifying rate (its job); service uses it.
        rateProvider.GetRateOnOrBeforeAsync("GBP", txDate, 6, Arg.Any<CancellationToken>())
            .Returns(new ExchangeRate(1.272m, new DateOnly(2024, 9, 30)));
        var service = BuildService(txRepo, rateProvider);

        var result = await service.GetAsync(transaction.Id, "GBP");

        Assert.Equal(1.272m, result.ExchangeRate);
        Assert.Equal(Math.Round(200.00m * 1.272m, 2, MidpointRounding.ToEven), result.ConvertedAmount);
    }
}
