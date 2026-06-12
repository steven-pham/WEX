using Wex.Cards.Application.Ports;
using Wex.Cards.Domain.Entities;

namespace Wex.Cards.Application.Cards.Commands;

public sealed class CreateCardService(ICardRepository repository)
{
    public async Task<CreateCardResult> ExecuteAsync(CreateCardCommand command, CancellationToken ct = default)
    {
        var card = Card.Create(command.CreditLimit ?? throw new ArgumentNullException(nameof(command)));
        await repository.AddAsync(card, ct);
        return new CreateCardResult(card.Id, card.CreditLimit.Amount);
    }
}
