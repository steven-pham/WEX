using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Wex.Cards.Domain.Exceptions;
using Wex.Cards.Infrastructure.ExchangeRates;

namespace Wex.Cards.UnitTests.ExchangeRates;

public sealed class TreasuryExchangeRateProviderTests
{
    // ---- helpers ----

    private static TreasuryExchangeRateProvider BuildProvider(
        HttpStatusCode status, string? json, out FakeHttpMessageHandler handler)
    {
        handler = new FakeHttpMessageHandler(status, json ?? "{}");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://fake.treasury.test/") };
        return new TreasuryExchangeRateProvider(httpClient);
    }

    private static string RateJson(string exchangeRate, string recordDate) =>
        $$"""{"data":[{"exchange_rate":"{{exchangeRate}}","record_date":"{{recordDate}}"}]}""";

    private static string EmptyJson() => """{"data":[]}""";

    // ---- latest rate ----

    [Fact]
    public async Task GetLatestRateAsync_KnownCurrency_ReturnsRate()
    {
        var sut = BuildProvider(HttpStatusCode.OK, RateJson("1.084", "2024-03-31"), out _);

        var result = await sut.GetLatestRateAsync("EUR");

        Assert.NotNull(result);
        Assert.Equal(1.084m, result.Rate);
        Assert.Equal(new DateOnly(2024, 3, 31), result.RecordDate);
    }

    [Fact]
    public async Task GetLatestRateAsync_EmptyData_ReturnsNull()
    {
        var sut = BuildProvider(HttpStatusCode.OK, EmptyJson(), out _);

        var result = await sut.GetLatestRateAsync("CAD");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestRateAsync_UnmappableIso_ReturnsNullWithoutHttpCall()
    {
        var sut = BuildProvider(HttpStatusCode.OK, EmptyJson(), out var handler);

        var result = await sut.GetLatestRateAsync("XYZ");

        Assert.Null(result);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task GetLatestRateAsync_UsdTarget_ReturnsUnitRateWithoutHttpCall()
    {
        var sut = BuildProvider(HttpStatusCode.OK, EmptyJson(), out var handler);

        var result = await sut.GetLatestRateAsync("USD");

        Assert.NotNull(result);
        Assert.Equal(1.0m, result.Rate);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task GetLatestRateAsync_ServerError_ThrowsExchangeRateUnavailableException()
    {
        var sut = BuildProvider(HttpStatusCode.ServiceUnavailable, null, out _);

        await Assert.ThrowsAsync<ExchangeRateUnavailableException>(
            () => sut.GetLatestRateAsync("GBP"));
    }

    // ---- on-or-before rate ----

    [Fact]
    public async Task GetRateOnOrBeforeAsync_MostRecentWithinWindow_ReturnsRate()
    {
        var date = new DateOnly(2024, 6, 30);
        var sut = BuildProvider(HttpStatusCode.OK, RateJson("1.272", "2024-06-30"), out _);

        var result = await sut.GetRateOnOrBeforeAsync("GBP", date);

        Assert.NotNull(result);
        Assert.Equal(1.272m, result.Rate);
        Assert.Equal(new DateOnly(2024, 6, 30), result.RecordDate);
    }

    [Fact]
    public async Task GetRateOnOrBeforeAsync_RateExactlyOnCutoff_ReturnsRate()
    {
        // Cutoff = date.AddMonths(-6) — inclusive; a rate on the cutoff date qualifies.
        var date = new DateOnly(2024, 6, 30);
        var cutoff = date.AddMonths(-6); // 2023-12-30
        var sut = BuildProvider(HttpStatusCode.OK, RateJson("0.92", cutoff.ToString("yyyy-MM-dd")), out _);

        var result = await sut.GetRateOnOrBeforeAsync("EUR", date);

        Assert.NotNull(result);
        Assert.Equal(0.92m, result.Rate);
        Assert.Equal(cutoff, result.RecordDate);
    }

    [Fact]
    public async Task GetRateOnOrBeforeAsync_NoQualifyingRate_ReturnsNull()
    {
        var sut = BuildProvider(HttpStatusCode.OK, EmptyJson(), out _);

        var result = await sut.GetRateOnOrBeforeAsync("CAD", new DateOnly(2024, 1, 1));

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRateOnOrBeforeAsync_UnmappableIso_ReturnsNullWithoutHttpCall()
    {
        var sut = BuildProvider(HttpStatusCode.OK, EmptyJson(), out var handler);

        var result = await sut.GetRateOnOrBeforeAsync("ZZZ", new DateOnly(2024, 1, 1));

        Assert.Null(result);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task GetRateOnOrBeforeAsync_UsdTarget_ReturnsUnitRateWithoutHttpCall()
    {
        var date = new DateOnly(2024, 3, 15);
        var sut = BuildProvider(HttpStatusCode.OK, EmptyJson(), out var handler);

        var result = await sut.GetRateOnOrBeforeAsync("USD", date);

        Assert.NotNull(result);
        Assert.Equal(1.0m, result.Rate);
        Assert.Equal(date, result.RecordDate);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task GetRateOnOrBeforeAsync_ServerError_ThrowsExchangeRateUnavailableException()
    {
        var sut = BuildProvider(HttpStatusCode.InternalServerError, null, out _);

        var ex = await Assert.ThrowsAsync<ExchangeRateUnavailableException>(
            () => sut.GetRateOnOrBeforeAsync("CAD", new DateOnly(2024, 1, 1)));

        // Exception is distinct from a null result (no qualifying rate).
        Assert.NotNull(ex);
    }

    // ---- resilience pipeline (retry) ----

    [Fact]
    public async Task GetLatestRateAsync_TransientServerError_ResiliencePipelineRetriesAndThrows()
    {
        // Arrange: wire the REAL AddStandardResilienceHandler pipeline through DI,
        // with delays zeroed so the test runs instantly.
        var handler = new FakeHttpMessageHandler(HttpStatusCode.ServiceUnavailable, "{}");

        var services = new ServiceCollection();
        services
            .AddHttpClient<TreasuryExchangeRateProvider>(c =>
                c.BaseAddress = new Uri("https://fake.treasury.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.Delay = TimeSpan.Zero;
                options.Retry.UseJitter = false;
            });

        var sp = services.BuildServiceProvider();
        var sut = sp.GetRequiredService<TreasuryExchangeRateProvider>();

        // Act & Assert: pipeline exhausts retries → ExchangeRateUnavailableException (not null).
        await Assert.ThrowsAsync<ExchangeRateUnavailableException>(
            () => sut.GetLatestRateAsync("EUR"));

        // The handler must have been called more than once, proving retries executed.
        Assert.True(handler.CallCount > 1,
            $"Resilience pipeline should retry transient failures; handler was called {handler.CallCount} time(s).");
    }
}

internal sealed class FakeHttpMessageHandler(HttpStatusCode status, string body) : HttpMessageHandler
{
    public int CallCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
