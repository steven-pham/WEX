namespace Wex.Cards.Domain.Exceptions;

public sealed class CardNotFoundException(Guid id) : Exception($"Card '{id}' was not found.");

public sealed class CardDomainException(string message) : Exception(message);
