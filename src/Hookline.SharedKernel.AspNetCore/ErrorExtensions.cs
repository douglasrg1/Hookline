using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Hookline.SharedKernel.AspNetCore;

public static class ErrorExtensions
{
    // A static field instead of a const: consts are copied into the caller at compile time,
    // which would hide a forbidden dependency from the architecture tests.
    public static readonly string CodeExtensionKey = "code";

    public static ProblemHttpResult ToProblem(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return TypedResults.Problem(
            detail: error.Message,
            statusCode: ToStatusCode(error.Type),
            extensions: new Dictionary<string, object?> { [CodeExtensionKey] = error.Code });
    }

    private static int ToStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported error type."),
    };
}
