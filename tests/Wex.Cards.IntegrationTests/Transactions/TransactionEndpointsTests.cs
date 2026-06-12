using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wex.Cards.Api.Cards;
using Wex.Cards.Api.Transactions;

namespace Wex.Cards.IntegrationTests.Transactions;

public sealed class TransactionEndpointsTests(CardApiFactory factory) : IClassFixture<CardApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<Guid> CreateCardAsync(decimal creditLimit = 1000m)
    {
        var response = await _client.PostAsJsonAsync("/cards", new CreateCardRequest(creditLimit));
        var body = await response.Content.ReadFromJsonAsync<CreateCardResponse>();
        return body!.Id;
    }

    [Fact]
    public async Task PostTransaction_ValidRequest_Returns201WithStoredTransaction()
    {
        var cardId = await CreateCardAsync();
        var request = new AddTransactionRequest("Coffee purchase", new DateOnly(2024, 1, 15), 5.75m);

        var response = await _client.PostAsJsonAsync($"/cards/{cardId}/transactions", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AddTransactionResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal(cardId, body.CardId);
        Assert.Equal("Coffee purchase", body.Description);
        Assert.Equal(new DateOnly(2024, 1, 15), body.TransactionDate);
        Assert.Equal(5.75m, body.Amount);
    }

    [Fact]
    public async Task PostTransaction_UnknownCard_Returns404()
    {
        var request = new AddTransactionRequest("Coffee", new DateOnly(2024, 1, 15), 5.00m);

        var response = await _client.PostAsJsonAsync($"/cards/{Guid.NewGuid()}/transactions", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTransaction_ExistingId_ReturnsOriginalAmount()
    {
        var cardId = await CreateCardAsync();
        var postResponse = await _client.PostAsJsonAsync($"/cards/{cardId}/transactions",
            new AddTransactionRequest("Fuel", new DateOnly(2024, 3, 10), 42.50m));
        var created = await postResponse.Content.ReadFromJsonAsync<AddTransactionResponse>();

        var getResponse = await _client.GetAsync($"/transactions/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var body = await getResponse.Content.ReadFromJsonAsync<GetTransactionResponse>();
        Assert.NotNull(body);
        Assert.Equal(created.Id, body.Id);
        Assert.Equal(cardId, body.CardId);
        Assert.Equal("Fuel", body.Description);
        Assert.Equal(new DateOnly(2024, 3, 10), body.TransactionDate);
        Assert.Equal(42.50m, body.Amount);
    }

    [Fact]
    public async Task GetTransaction_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/transactions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PostTransaction_NonPositiveAmount_Returns400WithProblemDetails(double amount)
    {
        var cardId = await CreateCardAsync();
        var request = new AddTransactionRequest("Coffee", new DateOnly(2024, 1, 15), (decimal)amount);

        var response = await _client.PostAsJsonAsync($"/cards/{cardId}/transactions", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(body);
        Assert.Equal(StatusCodes.Status400BadRequest, body.Status);
        Assert.NotEmpty(body.Errors);
    }

}
