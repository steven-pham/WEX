using Microsoft.AspNetCore.Mvc;
using Wex.Cards.Application.Cards.Commands;
using Wex.Cards.Application.Cards.Queries;

namespace Wex.Cards.Api.Cards;

[ApiController]
[Route("cards")]
public sealed class CardsController(
    CreateCardService createCardService,
    GetCardService getCardService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CreateCardResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCard(
        [FromBody] CreateCardRequest request,
        CancellationToken ct)
    {
        var command = new CreateCardCommand(request.CreditLimit);
        var result = await createCardService.ExecuteAsync(command, ct);
        var response = new CreateCardResponse(result.Id, result.CreditLimit);
        return CreatedAtAction(nameof(GetCard), new { id = result.Id }, response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<GetCardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCard(Guid id, CancellationToken ct)
    {
        var result = await getCardService.ExecuteAsync(new GetCardQuery(id), ct);
        return Ok(new GetCardResponse(result.Id, result.CreditLimit));
    }
}
