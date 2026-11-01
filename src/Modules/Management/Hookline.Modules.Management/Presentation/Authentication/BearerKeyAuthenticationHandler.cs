using System.Net.Http.Headers;
using System.Text.Encodings.Web;

using Hookline.Modules.Management.Application.Authentication;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hookline.Modules.Management.Presentation.Authentication;

// The API's only authentication scheme: "Authorization: Bearer <key>", where the key is the
// organization key or a consumer key. It protects the routes of every module.
internal sealed class BearerKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    KeyAuthenticator authenticator)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Bearer";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // No Authorization header, or another scheme: there is nothing to check here.
        // The authorization policy then answers 401 on every route that is not public.
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header)
            || !string.Equals(header.Scheme, SchemeName, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        if (string.IsNullOrWhiteSpace(header.Parameter))
        {
            return AuthenticateResult.Fail("The API key is missing.");
        }

        var caller = await authenticator.AuthenticateAsync(header.Parameter, Context.RequestAborted);

        if (caller is null)
        {
            return AuthenticateResult.Fail("The API key is not valid.");
        }

        var principal = CallerClaims.ToPrincipal(caller, Scheme.Name);

        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    // Tells the client which scheme the API expects (RFC 6750).
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = SchemeName;

        return base.HandleChallengeAsync(properties);
    }
}
