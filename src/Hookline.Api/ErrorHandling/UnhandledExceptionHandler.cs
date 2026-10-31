using Hookline.SharedKernel.AspNetCore;

using Microsoft.AspNetCore.Diagnostics;

namespace Hookline.Api.ErrorHandling;

internal sealed class UnhandledExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<UnhandledExceptionHandler> logger) : IExceptionHandler
{
    public const string ErrorCode = "internal_error";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Unhandled exception while processing {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // The exception is deliberately not passed to the problem details context:
        // the response must never expose messages or stack traces to API callers.
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Extensions = { [ErrorExtensions.CodeExtensionKey] = ErrorCode },
            },
        });
    }
}
