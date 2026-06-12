using Wex.Cards.Domain.Exceptions;
using Wex.Cards.Domain.ValueObjects;

namespace Wex.Cards.Domain.Entities;

public sealed class Transaction
{
    private Transaction() { }

    public Guid Id { get; private set; }
    public Guid CardId { get; private set; }
    public string Description { get; private set; } = null!;
    public DateOnly TransactionDate { get; private set; }
    public Money Amount { get; private set; } = null!;

    public static Transaction Create(Guid cardId, string description, DateOnly transactionDate, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new TransactionDomainException("Description is required.");

        if (amount <= 0)
            throw new TransactionDomainException("Amount must be greater than zero.");

        if (amount != Math.Round(amount, 4, MidpointRounding.AwayFromZero))
            throw new TransactionDomainException("Amount must have at most 4 decimal places.");

        return new Transaction
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            Description = description.Trim(),
            TransactionDate = transactionDate,
            Amount = Money.Usd(amount)
        };
    }
}
