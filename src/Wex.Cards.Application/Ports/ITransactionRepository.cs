using Wex.Cards.Domain.Entities;

namespace Wex.Cards.Application.Ports;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction, CancellationToken ct = default);
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<decimal> GetTotalSpentAsync(Guid cardId, CancellationToken ct = default);
}

