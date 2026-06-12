namespace Wex.Cards.Domain.Exceptions;

public sealed class ExchangeRateUnavailableException : Exception
{
    public ExchangeRateUnavailableException(string currency)
        : base($"The exchange rate service is unavailable for currency '{currency}'.") { }

    public ExchangeRateUnavailableException(string currency, Exception innerException)
        : base($"The exchange rate service is unavailable for currency '{currency}'.", innerException) { }
}
