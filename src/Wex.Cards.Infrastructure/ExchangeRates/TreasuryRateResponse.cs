using System.Text.Json.Serialization;

namespace Wex.Cards.Infrastructure.ExchangeRates;

internal sealed record TreasuryRateResponse(
    [property: JsonPropertyName("data")] TreasuryRateEntry[] Data
);

internal sealed record TreasuryRateEntry(
    [property: JsonPropertyName("exchange_rate")] string ExchangeRate,
    [property: JsonPropertyName("record_date")] string RecordDate
);
