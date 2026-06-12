namespace Wex.Cards.Infrastructure.ExchangeRates;

internal static class CurrencyDescriptionMap
{
    // Curated ISO 4217 → Treasury country_currency_desc mapping.
    // The dataset has no ISO field; this dictionary is the authoritative source
    // for which currencies the provider supports. Unrecognised codes return null.
    private static readonly Dictionary<string, string> Map =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["AUD"] = "Australia-Dollar",
            ["BRL"] = "Brazil-Real",
            ["CAD"] = "Canada-Dollar",
            ["CNY"] = "China-Renminbi",
            ["DKK"] = "Denmark-Krone",
            ["EUR"] = "Euro Zone-Euro",
            ["HKD"] = "Hong Kong-Dollar",
            ["INR"] = "India-Rupee",
            ["JPY"] = "Japan-Yen",
            ["KRW"] = "Korea-Won",
            ["MXN"] = "Mexico-Peso",
            ["NOK"] = "Norway-Krone",
            ["NZD"] = "New Zealand-Dollar",
            ["SEK"] = "Sweden-Krona",
            ["SGD"] = "Singapore-Dollar",
            ["CHF"] = "Switzerland-Franc",
            ["GBP"] = "United Kingdom-Pound",
            ["ZAR"] = "South Africa-Rand",
        };

    public static string? TryGet(string isoCode) =>
        Map.TryGetValue(isoCode, out var desc) ? desc : null;
}
