using Wex.Cards.Application.Ports;
using Wex.Cards.Application.Transactions.Commands;
using Wex.Cards.Application.Transactions.Queries;
using Wex.Cards.Domain.Entities;
using Wex.Cards.Domain.Exceptions;

namespace Wex.Cards.Application.Transactions;

public sealed class TransactionService(
    ICardRepository cardRepository,
    ITransactionRepository transactionRepository,
    IExchangeRateProvider exchangeRateProvider)
{
    public async Task<AddTransactionResult> AddAsync(AddTransactionCommand command, CancellationToken ct = default)
    {
        var card = await cardRepository.GetByIdAsync(command.CardId, ct);
        if (card is null)
            throw new CardNotFoundException(command.CardId);

        if (command.Description is null)
            throw new TransactionDomainException("Description is required.");
        if (command.TransactionDate is null)
            throw new TransactionDomainException("Transaction date is required.");
        if (command.Amount is null)
            throw new TransactionDomainException("Amount is required.");

        var transaction = Transaction.Create(
            command.CardId,
            command.Description,
            command.TransactionDate.Value,
            command.Amount.Value);

        await transactionRepository.AddAsync(transaction, ct);

        return new AddTransactionResult(
            transaction.Id,
            transaction.CardId,
            transaction.Description,
            transaction.TransactionDate,
            transaction.Amount.Amount);
    }

    public async Task<GetConvertedTransactionResult> GetAsync(
        Guid id, string? currency, CancellationToken ct = default)
    {
        var transaction = await transactionRepository.GetByIdAsync(id, ct);
        if (transaction is null)
            throw new TransactionNotFoundException(id);

        if (currency is null || currency.Equals("USD", StringComparison.OrdinalIgnoreCase))
        {
            return new GetConvertedTransactionResult(
                transaction.Id,
                transaction.CardId,
                transaction.Description,
                transaction.TransactionDate,
                transaction.Amount.Amount,
                1.0m,
                transaction.Amount.Amount);
        }

        var rate = await exchangeRateProvider.GetRateOnOrBeforeAsync(currency, transaction.TransactionDate, 6, ct);
        if (rate is null)
            throw new TransactionCurrencyConversionException(currency);

        var converted = Math.Round(transaction.Amount.Amount * rate.Rate, 2, MidpointRounding.ToEven);

        return new GetConvertedTransactionResult(
            transaction.Id,
            transaction.CardId,
            transaction.Description,
            transaction.TransactionDate,
            transaction.Amount.Amount,
            rate.Rate,
            converted);
    }
}
