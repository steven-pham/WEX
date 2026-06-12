using Wex.Cards.Application.Cards.Commands;
using Wex.Cards.Application.Cards.Queries;
using Wex.Cards.Application.Ports;
using Wex.Cards.Domain.Entities;
using Wex.Cards.Domain.Exceptions;

namespace Wex.Cards.Application.Cards;

public sealed class CardService(ICardRepository repository)
{
    public async Task<CreateCardResult> CreateAsync(CreateCardCommand command, CancellationToken ct = default)
    {
        var card = Card.Create(command.CreditLimit ?? throw new ArgumentNullException(nameof(command)));
        await repository.AddAsync(card, ct);
        return new CreateCardResult(card.Id, card.CreditLimit.Amount);
    }

    public async Task<GetCardResult> GetAsync(Guid id, CancellationToken ct = default)
    {
        var card = await repository.GetByIdAsync(id, ct);
        if (card is null)
            throw new CardNotFoundException(id);
        return new GetCardResult(card.Id, card.CreditLimit.Amount);
    }
}
