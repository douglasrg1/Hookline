using Hookline.SharedKernel.AspNetCore;

using Microsoft.AspNetCore.Diagnostics;

namespace Hookline.Api.ErrorHandling;

// Registered before UnhandledExceptionHandler: a malformed request is the client's fault,
// so it keeps its own 4xx status instead of becoming a 500.
internal sealed class BadHttpRequestExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<BadHttpRequestExceptionHandler> logger) : IExceptionHandler
{
    public const string ErrorCode = "bad_request";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException badRequest)
        {
            return false;
        }

        logger.LogWarning(
            "Bad request while processing {Method} {Path}: {Reason}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            badRequest.Message);

        httpContext.Response.StatusCode = badRequest.StatusCode;

        // The exception message stays in the log only: it can name internal parameters and types.
        // The title is left empty so ASP.NET fills the standard one for the status code.
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = badRequest.StatusCode,
                Extensions = { [ErrorExtensions.CodeExtensionKey] = ErrorCode },
            },
        });
    }
}
