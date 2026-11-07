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
        var conflict = exception as AdminConflictException;
        if (conflict is null)
        {
            logger.LogError(exception, "Unhandled exception while processing {Path}.", httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning("Admin conflict while processing {Path}: {Message}", httpContext.Request.Path, conflict.Message);
        }

        var statusCode = conflict is null ? StatusCodes.Status500InternalServerError : StatusCodes.Status409Conflict;
        httpContext.Response.StatusCode = statusCode;
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = conflict is null ? "An unexpected error occurred." : "Conflict",
                Detail = conflict?.Message ?? "The request could not be completed.",
                Instance = httpContext.Request.Path
            }
        });

        return true;
    }
}
