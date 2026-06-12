namespace Wex.Cards.Domain.Exceptions;

public sealed class TransactionNotFoundException(Guid id) : Exception($"Transaction '{id}' was not found.");

public sealed class TransactionDomainException(string message) : Exception(message);
