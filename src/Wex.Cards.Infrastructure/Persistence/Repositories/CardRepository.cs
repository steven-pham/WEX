using Microsoft.EntityFrameworkCore;
using Wex.Cards.Application.Ports;
using Wex.Cards.Domain.Entities;

namespace Wex.Cards.Infrastructure.Persistence.Repositories;

internal sealed class CardRepository(CardsDbContext context) : ICardRepository
{
    public async Task AddAsync(Card card, CancellationToken ct = default)
    {
        context.Cards.Add(card);
        await context.SaveChangesAsync(ct);
    }

    public async Task<Card?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
    }
}
