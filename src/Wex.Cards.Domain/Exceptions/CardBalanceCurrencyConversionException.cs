namespace Wex.Cards.Domain.Exceptions;

public sealed class CardBalanceCurrencyConversionException(string currency)
    : Exception($"Balance cannot be converted to the target currency '{currency}'.");
