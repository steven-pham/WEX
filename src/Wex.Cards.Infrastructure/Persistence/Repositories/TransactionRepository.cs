using Microsoft.EntityFrameworkCore;
using Wex.Cards.Application.Ports;
using Wex.Cards.Domain.Entities;

namespace Wex.Cards.Infrastructure.Persistence.Repositories;

internal sealed class TransactionRepository(CardsDbContext context) : ITransactionRepository
{
    public async Task AddAsync(Transaction transaction, CancellationToken ct = default)
    {
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync(ct);
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }
}
