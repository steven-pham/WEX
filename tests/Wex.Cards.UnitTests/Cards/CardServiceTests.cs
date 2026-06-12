using NSubstitute;
using Wex.Cards.Application.Cards;
using Wex.Cards.Application.Cards.Commands;
using Wex.Cards.Application.Ports;
using Wex.Cards.Domain.Entities;
using Wex.Cards.Domain.Exceptions;

namespace Wex.Cards.UnitTests.Cards;

public sealed class CardServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidCommand_AddsCardAndReturnsResult()
    {
        var repo = Substitute.For<ICardRepository>();
        var service = new CardService(repo);
        var command = new CreateCardCommand(500m);

        var result = await service.CreateAsync(command);

        Assert.Equal(500m, result.CreditLimit);
        Assert.NotEqual(Guid.Empty, result.Id);
        await repo.Received(1).AddAsync(Arg.Any<Card>());
    }

    [Fact]
    public async Task GetAsync_ExistingId_ReturnsResult()
    {
        var card = Card.Create(300m);
        var repo = Substitute.For<ICardRepository>();
        repo.GetByIdAsync(card.Id).Returns(card);
        var service = new CardService(repo);

        var result = await service.GetAsync(card.Id);

        Assert.Equal(card.Id, result.Id);
        Assert.Equal(300m, result.CreditLimit);
    }

    [Fact]
    public async Task GetAsync_UnknownId_ThrowsCardNotFoundException()
    {
        var repo = Substitute.For<ICardRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>()).Returns((Card?)null);
        var service = new CardService(repo);

        await Assert.ThrowsAsync<CardNotFoundException>(() => service.GetAsync(Guid.NewGuid()));
    }
}
