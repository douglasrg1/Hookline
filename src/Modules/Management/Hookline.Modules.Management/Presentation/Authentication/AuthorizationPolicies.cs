namespace Hookline.Modules.Management.Presentation.Authentication;

internal static class AuthorizationPolicies
{
    // Routes only the organization may call. A consumer gets 403: the route exists, just not for it.
    public const string Organization = "organization";
}
