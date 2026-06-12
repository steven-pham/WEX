using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Wex.Cards.Api.Cards;
using Wex.Cards.Api.Transactions;
using Wex.Cards.Application.Ports;
using Wex.Cards.Domain.Exceptions;

namespace Wex.Cards.IntegrationTests.Transactions;

public sealed class TransactionCurrencyConversionEndpointTests(CurrencyConversionApiFactory factory)
    : IClassFixture<CurrencyConversionApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly IExchangeRateProvider _rateProvider = factory.ExchangeRateProvider;

    private async Task<Guid> CreateCardAsync(decimal creditLimit = 1000m)
    {
        var response = await _client.PostAsJsonAsync("/cards", new CreateCardRequest(creditLimit));
        var body = await response.Content.ReadFromJsonAsync<CreateCardResponse>();
        return body!.Id;
    }

    private async Task<AddTransactionResponse> CreateTransactionAsync(Guid cardId, DateOnly date, decimal amount)
    {
        var response = await _client.PostAsJsonAsync(
            $"/cards/{cardId}/transactions",
            new AddTransactionRequest("Test purchase", date, amount));
        return (await response.Content.ReadFromJsonAsync<AddTransactionResponse>())!;
    }

    // ---- converted response shape ----

    [Fact]
    public async Task GetTransaction_WithCurrency_ReturnsConvertedResponseShape()
    {
        var cardId = await CreateCardAsync();
        var txDate = new DateOnly(2024, 6, 15);
        var tx = await CreateTransactionAsync(cardId, txDate, 100.00m);

        _rateProvider.GetRateOnOrBeforeAsync("EUR", txDate, 6, Arg.Any<CancellationToken>())
            .Returns(new ExchangeRate(1.084m, new DateOnly(2024, 6, 1)));

        var response = await _client.GetAsync($"/transactions/{tx.Id}?currency=EUR");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetConvertedTransactionResponse>();
        Assert.NotNull(body);
        Assert.Equal(tx.Id, body.Id);
        Assert.Equal(cardId, body.CardId);
        Assert.Equal("Test purchase", body.Description);
        Assert.Equal(txDate, body.TransactionDate);
        Assert.Equal(100.00m, body.OriginalAmount);
        Assert.Equal(1.084m, body.ExchangeRate);
        Assert.Equal(108.40m, body.ConvertedAmount);
    }

    [Fact]
    public async Task GetTransaction_WithoutCurrency_ReturnsUnifiedShapeWithBaseRateAndOriginalAmount()
    {
        var cardId = await CreateCardAsync();
        var tx = await CreateTransactionAsync(cardId, new DateOnly(2024, 3, 10), 42.50m);

        var response = await _client.GetAsync($"/transactions/{tx.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetConvertedTransactionResponse>();
        Assert.NotNull(body);
        Assert.Equal(tx.Id, body.Id);
        Assert.Equal(42.50m, body.OriginalAmount);
        Assert.Equal(1.0m, body.ExchangeRate);
        Assert.Equal(42.50m, body.ConvertedAmount);
    }

    // ---- unknown transaction → 404 ----

    [Fact]
    public async Task GetTransaction_UnknownId_WithCurrency_Returns404()
    {
        var response = await _client.GetAsync($"/transactions/{Guid.NewGuid()}?currency=EUR");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- no qualifying rate → 422 "cannot convert" ----

    [Fact]
    public async Task GetTransaction_NoQualifyingRate_Returns422ProblemDetails()
    {
        var cardId = await CreateCardAsync();
        var txDate = new DateOnly(2024, 6, 15);
        var tx = await CreateTransactionAsync(cardId, txDate, 50.00m);

        _rateProvider.GetRateOnOrBeforeAsync("GBP", txDate, 6, Arg.Any<CancellationToken>())
            .Returns((ExchangeRate?)null);

        var response = await _client.GetAsync($"/transactions/{tx.Id}?currency=GBP");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(body);
        Assert.Equal(422, body.Status);
    }

    // ---- provider failure → 5xx, NOT 422 ----

    [Fact]
    public async Task GetTransaction_ProviderFailure_Returns5xxNotUnprocessableEntity()
    {
        var cardId = await CreateCardAsync();
        var txDate = new DateOnly(2024, 6, 15);
        var tx = await CreateTransactionAsync(cardId, txDate, 75.00m);

        _rateProvider.GetRateOnOrBeforeAsync("CAD", txDate, 6, Arg.Any<CancellationToken>())
            .Throws(new ExchangeRateUnavailableException("CAD"));

        var response = await _client.GetAsync($"/transactions/{tx.Id}?currency=CAD");

        Assert.NotEqual(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.True(
            (int)response.StatusCode >= 500,
            $"Expected 5xx but got {(int)response.StatusCode}");
    }
}
