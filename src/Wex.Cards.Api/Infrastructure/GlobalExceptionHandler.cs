using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Wex.Cards.Domain.Exceptions;

namespace Wex.Cards.Api.Infrastructure;

internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            CardNotFoundException or TransactionNotFoundException =>
                (StatusCodes.Status404NotFound, "Resource not found.", exception.Message),
            CardDomainException or TransactionDomainException =>
                (StatusCodes.Status400BadRequest, "Invalid request.", exception.Message),
            TransactionCurrencyConversionException or CardBalanceCurrencyConversionException =>
                (StatusCodes.Status422UnprocessableEntity, "Unsupported currency.", exception.Message),
            ExchangeRateUnavailableException =>
                (StatusCodes.Status503ServiceUnavailable, "Exchange rate service unavailable.", "An upstream exchange rate service is temporarily unavailable. Please try again later."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null as string)
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception.");

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        }, cancellationToken);

        return true;
    }
}
