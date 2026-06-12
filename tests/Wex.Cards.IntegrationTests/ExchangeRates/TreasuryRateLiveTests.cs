using Wex.Cards.Infrastructure.ExchangeRates;

namespace Wex.Cards.IntegrationTests.ExchangeRates;

/// <summary>
/// Live smoke tests against the public Treasury Reporting Rates of Exchange API.
/// Asserts response shape only, not exact values.
/// </summary>
public sealed class TreasuryRateLiveTests
{
    private static TreasuryExchangeRateProvider BuildLiveProvider()
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(
                "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/accounting/od/rates_of_exchange")
        };
        return new TreasuryExchangeRateProvider(httpClient);
    }

    [Fact]
    public async Task GetLatestRateAsync_LiveApi_ReturnsValidShape()
    {
        var sut = BuildLiveProvider();

        var result = await sut.GetLatestRateAsync("EUR");

        Assert.NotNull(result);
        Assert.True(result.Rate > 0, "Rate must be positive.");
        Assert.True(result.RecordDate <= DateOnly.FromDateTime(DateTime.UtcNow),
            "RecordDate must not be in the future.");
    }

    [Fact]
    public async Task GetRateOnOrBeforeAsync_LiveApi_ReturnsValidShape()
    {
        var sut = BuildLiveProvider();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var result = await sut.GetRateOnOrBeforeAsync("GBP", today);

        Assert.NotNull(result);
        Assert.True(result.Rate > 0, "Rate must be positive.");
        Assert.True(result.RecordDate <= today, "RecordDate must be on or before the requested date.");
    }
}
