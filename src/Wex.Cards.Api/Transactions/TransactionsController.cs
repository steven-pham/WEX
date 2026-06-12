using Microsoft.AspNetCore.Mvc;
using Wex.Cards.Application.Transactions;
using Wex.Cards.Application.Transactions.Commands;

namespace Wex.Cards.Api.Transactions;

[ApiController]
public sealed class TransactionsController(TransactionService transactionService) : ControllerBase
{
    [HttpPost("cards/{cardId:guid}/transactions")]
    [ProducesResponseType<AddTransactionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddTransaction(
        Guid cardId,
        [FromBody] AddTransactionRequest request,
        CancellationToken ct)
    {
        var command = new AddTransactionCommand(cardId, request.Description, request.TransactionDate, request.Amount);
        var result = await transactionService.AddAsync(command, ct);
        var response = new AddTransactionResponse(result.Id, result.CardId, result.Description, result.TransactionDate, result.Amount);
        return CreatedAtAction(nameof(GetTransaction), new { id = result.Id }, response);
    }

    [HttpGet("transactions/{id:guid}")]
    [ProducesResponseType<GetTransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransaction(Guid id, CancellationToken ct)
    {
        var result = await transactionService.GetAsync(id, ct);
        return Ok(new GetTransactionResponse(result.Id, result.CardId, result.Description, result.TransactionDate, result.Amount));
    }
}
