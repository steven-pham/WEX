using System.Globalization;
using System.Text.Json;
using Wex.Cards.Application.Ports;
using Wex.Cards.Domain.Exceptions;

namespace Wex.Cards.Infrastructure.ExchangeRates;

internal sealed class TreasuryExchangeRateProvider(HttpClient httpClient) : IExchangeRateProvider
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public Task<ExchangeRate?> GetLatestRateAsync(string currency, CancellationToken ct = default)
    {
        if (string.Equals(currency, "USD", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<ExchangeRate?>(
                new ExchangeRate(1.0m, DateOnly.FromDateTime(DateTime.UtcNow)));

        var desc = CurrencyDescriptionMap.TryGet(currency);
        if (desc is null) return Task.FromResult<ExchangeRate?>(null);

        var url = BuildUrl(desc, dateFilter: null);
        return FetchFirstRateAsync(url, currency, ct);
    }

    public Task<ExchangeRate?> GetRateOnOrBeforeAsync(
        string currency, DateOnly date, int months = 6, CancellationToken ct = default)
    {
        if (string.Equals(currency, "USD", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<ExchangeRate?>(new ExchangeRate(1.0m, date));

        var desc = CurrencyDescriptionMap.TryGet(currency);
        if (desc is null) return Task.FromResult<ExchangeRate?>(null);

        var cutoff = date.AddMonths(-months);
        var dateFilter =
            $",record_date:lte:{date:yyyy-MM-dd},record_date:gte:{cutoff:yyyy-MM-dd}";

        var url = BuildUrl(desc, dateFilter);
        return FetchFirstRateAsync(url, currency, ct);
    }

    private string BuildUrl(string desc, string? dateFilter)
    {
        // Build the full absolute URL from BaseAddress so we bypass HttpClient's
        // BaseAddress + relative-URI merge, which encodes brackets in page[size].
        // Pre-encode brackets (%5B/%5D) ourselves so the Uri constructor doesn't
        // double-encode or strip them — Treasury API accepts both forms.
        var base_ = httpClient.BaseAddress!.OriginalString.TrimEnd('/');
        var encodedDesc = Uri.EscapeDataString(desc);
        return base_ +
               $"?filter=country_currency_desc:eq:{encodedDesc}{dateFilter}" +
               "&sort=-record_date&page%5Bsize%5D=1&fields=record_date,exchange_rate";
    }

    private async Task<ExchangeRate?> FetchFirstRateAsync(
        string url, string currency, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new ExchangeRateUnavailableException(currency, ex);
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<TreasuryRateResponse>(body, JsonOptions);

        if (result?.Data is not { Length: > 0 })
            return null;

        var entry = result.Data[0];

        if (!decimal.TryParse(
                entry.ExchangeRate,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var rate))
            return null;

        if (!DateOnly.TryParseExact(
                entry.RecordDate, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var recordDate))
            return null;

        return new ExchangeRate(rate, recordDate);
    }
}
