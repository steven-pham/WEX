using Wex.Cards.Application.Ports;
using Wex.Cards.Domain.Exceptions;

namespace Wex.Cards.Application.Cards.Queries;

public sealed class GetCardService(ICardRepository repository)
{
    public async Task<GetCardResult> ExecuteAsync(GetCardQuery query, CancellationToken ct = default)
    {
        var card = await repository.GetByIdAsync(query.Id, ct);
        if (card is null)
            throw new CardNotFoundException(query.Id);
        return new GetCardResult(card.Id, card.CreditLimit.Amount);
    }
}
