using System.Security.Claims;

using Hookline.Modules.Management.Application.Authentication;

namespace Hookline.Modules.Management.Presentation.Authentication;

// How the caller is written into the request's user, for authorization rules and routes to read.
internal static class CallerClaims
{
    public const string OrganizationRole = "organization";
    public const string ConsumerRole = "consumer";
    public const string ConsumerIdType = "consumer_id";

    public static ClaimsPrincipal ToPrincipal(Caller caller, string authenticationType)
    {
        Claim[] claims = caller.ConsumerId is { } consumerId
            ? [new(ClaimTypes.Role, ConsumerRole), new(ConsumerIdType, consumerId.ToString())]
            : [new(ClaimTypes.Role, OrganizationRole)];

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType));
    }

    // The way back. Null when the request was not authenticated by the API's scheme.
    public static Caller? FromPrincipal(ClaimsPrincipal principal)
    {
        if (principal.IsInRole(OrganizationRole))
        {
            return Caller.Organization;
        }

        if (principal.IsInRole(ConsumerRole) && Guid.TryParse(principal.FindFirstValue(ConsumerIdType), out var consumerId))
        {
            return Caller.Consumer(consumerId);
        }

        return null;
    }
}
