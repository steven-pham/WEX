namespace Wex.Cards.Application.Ports;

public record ExchangeRate(decimal Rate, DateOnly RecordDate);

public interface IExchangeRateProvider
{
    Task<ExchangeRate?> GetLatestRateAsync(string currency, CancellationToken ct = default);
    Task<ExchangeRate?> GetRateOnOrBeforeAsync(string currency, DateOnly date, int months = 6, CancellationToken ct = default);
}
