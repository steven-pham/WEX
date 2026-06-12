using Wex.Cards.Application.Cards.Commands;
using Wex.Cards.Application.Cards.Queries;
using Wex.Cards.Application.Ports;
using Wex.Cards.Domain.Entities;
using Wex.Cards.Domain.Exceptions;

namespace Wex.Cards.Application.Cards;

public sealed class CardService(
    ICardRepository cardRepository,
    ITransactionRepository transactionRepository,
    IExchangeRateProvider exchangeRateProvider)
{
    public async Task<CreateCardResult> CreateAsync(CreateCardCommand command, CancellationToken ct = default)
    {
        if (command.CreditLimit is null)
            throw new CardDomainException("Credit limit is required.");
        var card = Card.Create(command.CreditLimit.Value);
        await cardRepository.AddAsync(card, ct);
        return new CreateCardResult(card.Id, card.CreditLimit.Amount);
    }

    public async Task<GetCardResult> GetAsync(Guid id, CancellationToken ct = default)
    {
        var card = await cardRepository.GetByIdAsync(id, ct);
        if (card is null)
            throw new CardNotFoundException(id);
        return new GetCardResult(card.Id, card.CreditLimit.Amount);
    }

    public async Task<GetCardBalanceResult> GetBalanceAsync(Guid id, string? currency, CancellationToken ct = default)
    {
        var card = await cardRepository.GetByIdAsync(id, ct);
        if (card is null)
            throw new CardNotFoundException(id);

        var totalSpent = await transactionRepository.GetTotalSpentAsync(id, ct);
        var availableBalance = card.CreditLimit.Amount - totalSpent;

        var trimmed = currency?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Equals("USD", StringComparison.OrdinalIgnoreCase))
            return new GetCardBalanceResult(availableBalance, 1.0m, availableBalance);

        var rate = await exchangeRateProvider.GetLatestRateAsync(trimmed, ct);
        if (rate is null)
            throw new CardBalanceCurrencyConversionException(trimmed);

        var converted = Math.Round(availableBalance * rate.Rate, 2, MidpointRounding.ToEven);
        return new GetCardBalanceResult(availableBalance, rate.Rate, converted);
    }
}
