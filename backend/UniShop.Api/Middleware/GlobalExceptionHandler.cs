using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UniShop.Api.Services;

namespace UniShop.Api.Middleware;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var conflictMessage = exception switch
        {
            AdminConflictException adminConflict => adminConflict.Message,
            ShoppingConflictException shoppingConflict => shoppingConflict.Message,
            _ => null
        };
        var unavailableMessage = exception is PaymentProviderUnavailableException unavailable ? unavailable.Message : null;
        if (exception is PaymentProviderUnavailableException)
        {
            logger.LogError(exception, "Payment provider is unavailable while processing {Path}.", httpContext.Request.Path);
        }
        else if (conflictMessage is null)
        {
            logger.LogError(exception, "Unhandled exception while processing {Path}.", httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning("Conflict while processing {Path}: {Message}", httpContext.Request.Path, conflictMessage);
        }

        var statusCode = conflictMessage is not null
            ? StatusCodes.Status409Conflict
            : unavailableMessage is not null
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status500InternalServerError;
        httpContext.Response.StatusCode = statusCode;
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = conflictMessage is not null ? "Conflict" : unavailableMessage is not null ? "Payment temporarily unavailable" : "An unexpected error occurred.",
                Detail = conflictMessage ?? (unavailableMessage is not null
                    ? "Card payments are temporarily unavailable. Please choose cash on delivery or try again later."
                    : "The request could not be completed."),
                Instance = httpContext.Request.Path
            }
        });

        return true;
    }
}
