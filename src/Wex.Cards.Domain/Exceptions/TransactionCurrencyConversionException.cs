namespace Wex.Cards.Domain.Exceptions;

public sealed class TransactionCurrencyConversionException(string currency)
    : Exception($"Transaction cannot be converted to the target currency '{currency}'.");
