using Wex.Cards.Domain.Entities;

namespace Wex.Cards.Application.Ports;

public interface ICardRepository
{
    Task AddAsync(Card card, CancellationToken ct = default);
    Task<Card?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
