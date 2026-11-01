using Hookline.SharedKernel.AspNetCore;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hookline.Modules.Management.Presentation.Authentication;

// Guards the /consumers/{consumerId} route group: the request reaches the route only when the caller
// may access that consumer. Otherwise it gets the same 404 as a consumer that does not exist.
internal sealed class ConsumerAccessFilter : IEndpointFilter
{
    public const string RouteParameter = "consumerId";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        // The fallback policy and the route constraint make both cases impossible inside the group.
        // Reaching them means the filter was placed on the wrong routes, which is a bug, not a client error.
        var caller = CallerClaims.FromPrincipal(httpContext.User)
            ?? throw new InvalidOperationException("The consumer access filter needs an authenticated caller.");

        if (!Guid.TryParse(httpContext.GetRouteValue(RouteParameter) as string, out var consumerId))
        {
            throw new InvalidOperationException($"The consumer access filter needs a '{RouteParameter}' route value.");
        }

        var access = caller.CheckAccess(consumerId);

        return access.IsSuccess ? await next(context) : access.Error.ToProblem();
    }
}
