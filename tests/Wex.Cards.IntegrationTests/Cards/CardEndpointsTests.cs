using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wex.Cards.Api.Cards;

namespace Wex.Cards.IntegrationTests.Cards;

public sealed class CardEndpointsTests(CardApiFactory factory) : IClassFixture<CardApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PostCards_ValidRequest_Returns201WithIdAndCreditLimit()
    {
        var response = await _client.PostAsJsonAsync("/cards", new CreateCardRequest(500.00m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateCardResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal(500.00m, body.CreditLimit);
    }

    [Fact]
    public async Task GetCard_ExistingId_Returns200WithStoredCard()
    {
        var createResponse = await _client.PostAsJsonAsync("/cards", new CreateCardRequest(250.00m));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateCardResponse>();

        var getResponse = await _client.GetAsync($"/cards/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var body = await getResponse.Content.ReadFromJsonAsync<GetCardResponse>();
        Assert.NotNull(body);
        Assert.Equal(created.Id, body.Id);
        Assert.Equal(250.00m, body.CreditLimit);
    }

    [Fact]
    public async Task GetCard_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/cards/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1000)]
    public async Task PostCards_NegativeCreditLimit_Returns400WithProblemDetails(double limit)
    {
        var response = await _client.PostAsJsonAsync("/cards", new CreateCardRequest((decimal)limit));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(body);
        Assert.Equal(StatusCodes.Status400BadRequest, body.Status);
        Assert.NotEmpty(body.Errors);
    }

    [Theory]
    [InlineData("1000.123")]
    [InlineData("0.001")]
    public async Task PostCards_CreditLimitExceedsMaxDecimalPlaces_Returns400WithProblemDetails(string input)
    {
        var response = await _client.PostAsJsonAsync("/cards", new CreateCardRequest(decimal.Parse(input)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(body);
        Assert.Equal(StatusCodes.Status400BadRequest, body.Status);
        Assert.NotEmpty(body.Errors);
    }

    [Fact]
    public async Task GetCard_MalformedGuid_Returns404()
    {
        var response = await _client.GetAsync("/cards/not-a-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
