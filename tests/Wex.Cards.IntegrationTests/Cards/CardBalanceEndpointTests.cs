using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Wex.Cards.Api.Cards;
using Wex.Cards.Application.Ports;

namespace Wex.Cards.IntegrationTests.Cards;

public sealed class CardBalanceEndpointTests(CurrencyConversionApiFactory factory)
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

    private async Task AddTransactionAsync(Guid cardId, decimal amount)
    {
        var response = await _client.PostAsJsonAsync(
            $"/cards/{cardId}/transactions",
            new { description = "Test purchase", transactionDate = "2024-06-15", amount });
        response.EnsureSuccessStatusCode();
    }

    // ---- balance reflects transactions ----

    [Fact]
    public async Task GetBalance_NoTransactions_ReturnsCreditLimit()
    {
        var cardId = await CreateCardAsync(1000m);

        var response = await _client.GetAsync($"/cards/{cardId}/balance");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetCardBalanceResponse>();
        Assert.NotNull(body);
        Assert.Equal(1000m, body.AvailableBalance);
        Assert.Equal(1.0m, body.ExchangeRate);
        Assert.Equal(1000m, body.ConvertedBalance);
    }

    [Fact]
    public async Task GetBalance_WithTransactions_ReturnsReducedBalance()
    {
        var cardId = await CreateCardAsync(1000m);
        await AddTransactionAsync(cardId, 250m);
        await AddTransactionAsync(cardId, 100m);

        var response = await _client.GetAsync($"/cards/{cardId}/balance");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetCardBalanceResponse>();
        Assert.NotNull(body);
        Assert.Equal(650m, body.AvailableBalance);
    }

    // ---- converted payload shape ----

    [Fact]
    public async Task GetBalance_WithCurrency_ReturnsConvertedPayloadShape()
    {
        var cardId = await CreateCardAsync(500m);
        await AddTransactionAsync(cardId, 100m);

        _rateProvider.GetLatestRateAsync("EUR", Arg.Any<CancellationToken>())
            .Returns(new ExchangeRate(1.084m, new DateOnly(2024, 6, 1)));

        var response = await _client.GetAsync($"/cards/{cardId}/balance?currency=EUR");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetCardBalanceResponse>();
        Assert.NotNull(body);
        Assert.Equal(400m, body.AvailableBalance);
        Assert.Equal(1.084m, body.ExchangeRate);
        Assert.Equal(Math.Round(400m * 1.084m, 2, MidpointRounding.ToEven), body.ConvertedBalance);
    }

    // ---- unknown card → 404 ----

    [Fact]
    public async Task GetBalance_UnknownCard_Returns404()
    {
        var response = await _client.GetAsync($"/cards/{Guid.NewGuid()}/balance");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- unsupported currency → 422 ----

    [Fact]
    public async Task GetBalance_UnsupportedCurrency_Returns422ProblemDetails()
    {
        var cardId = await CreateCardAsync(500m);

        _rateProvider.GetLatestRateAsync("XYZ", Arg.Any<CancellationToken>())
            .Returns((ExchangeRate?)null);

        var response = await _client.GetAsync($"/cards/{cardId}/balance?currency=XYZ");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(body);
        Assert.Equal(422, body.Status);
    }
}
