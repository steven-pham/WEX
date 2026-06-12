namespace Wex.Cards.Domain.Exceptions;

public sealed class CardNotFoundException(Guid id) : Exception($"Card '{id}' was not found.");
