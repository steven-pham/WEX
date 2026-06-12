using NSubstitute;
using Wex.Cards.Application.Ports;
using Wex.Cards.Application.Transactions;
using Wex.Cards.Application.Transactions.Commands;
using Wex.Cards.Domain.Entities;
using Wex.Cards.Domain.Exceptions;

namespace Wex.Cards.UnitTests.Transactions;

public sealed class TransactionServiceTests
{
    [Fact]
    public async Task AddAsync_UnknownCard_ThrowsCardNotFoundException()
    {
        var cardRepo = Substitute.For<ICardRepository>();
        var txRepo = Substitute.For<ITransactionRepository>();
        cardRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Card?)null);
        var service = new TransactionService(cardRepo, txRepo);
        var command = new AddTransactionCommand(Guid.NewGuid(), "Coffee", new DateOnly(2024, 1, 15), 5.00m);

        await Assert.ThrowsAsync<CardNotFoundException>(() => service.AddAsync(command));
    }

    [Fact]
    public async Task AddAsync_ValidCommand_AddsTransactionAndReturnsResult()
    {
        var card = Card.Create(1000m);
        var cardRepo = Substitute.For<ICardRepository>();
        var txRepo = Substitute.For<ITransactionRepository>();
        cardRepo.GetByIdAsync(card.Id).Returns(card);
        var service = new TransactionService(cardRepo, txRepo);
        var command = new AddTransactionCommand(card.Id, "Coffee", new DateOnly(2024, 1, 15), 5.75m);

        var result = await service.AddAsync(command);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(card.Id, result.CardId);
        Assert.Equal("Coffee", result.Description);
        Assert.Equal(new DateOnly(2024, 1, 15), result.TransactionDate);
        Assert.Equal(5.75m, result.Amount);
        await txRepo.Received(1).AddAsync(Arg.Any<Transaction>());
    }

    [Fact]
    public async Task GetAsync_ExistingId_ReturnsResult()
    {
        var cardId = Guid.NewGuid();
        var transaction = Transaction.Create(cardId, "Fuel", new DateOnly(2024, 3, 10), 42.50m);
        var cardRepo = Substitute.For<ICardRepository>();
        var txRepo = Substitute.For<ITransactionRepository>();
        txRepo.GetByIdAsync(transaction.Id).Returns(transaction);
        var service = new TransactionService(cardRepo, txRepo);

        var result = await service.GetAsync(transaction.Id);

        Assert.Equal(transaction.Id, result.Id);
        Assert.Equal(cardId, result.CardId);
        Assert.Equal("Fuel", result.Description);
        Assert.Equal(new DateOnly(2024, 3, 10), result.TransactionDate);
        Assert.Equal(42.50m, result.Amount);
    }

    [Fact]
    public async Task GetAsync_UnknownId_ThrowsTransactionNotFoundException()
    {
        var cardRepo = Substitute.For<ICardRepository>();
        var txRepo = Substitute.For<ITransactionRepository>();
        txRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Transaction?)null);
        var service = new TransactionService(cardRepo, txRepo);

        await Assert.ThrowsAsync<TransactionNotFoundException>(() => service.GetAsync(Guid.NewGuid()));
    }
}
