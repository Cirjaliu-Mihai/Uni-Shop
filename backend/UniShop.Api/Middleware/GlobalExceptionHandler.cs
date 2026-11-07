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
        if (conflictMessage is null)
        {
            logger.LogError(exception, "Unhandled exception while processing {Path}.", httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning("Conflict while processing {Path}: {Message}", httpContext.Request.Path, conflictMessage);
        }

        var statusCode = conflictMessage is null ? StatusCodes.Status500InternalServerError : StatusCodes.Status409Conflict;
        httpContext.Response.StatusCode = statusCode;
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = conflictMessage is null ? "An unexpected error occurred." : "Conflict",
                Detail = conflictMessage ?? "The request could not be completed.",
                Instance = httpContext.Request.Path
            }
        });

        return true;
    }
}
