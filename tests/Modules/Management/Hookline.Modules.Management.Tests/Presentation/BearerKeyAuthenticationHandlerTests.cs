using System.Security.Claims;
using System.Text.Encodings.Web;

using Hookline.Modules.Management.Application.Authentication;
using Hookline.Modules.Management.Domain.ConsumerKeys;
using Hookline.Modules.Management.Presentation.Authentication;
using Hookline.Modules.Management.Tests.Fakes;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hookline.Modules.Management.Tests.Presentation;

public class BearerKeyAuthenticationHandlerTests
{
    private const string OrganizationKey = "organization-key";

    private static readonly DateTimeOffset Now = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Request_without_a_key_is_left_to_the_authorization_policy()
    {
        var result = await AuthenticateAsync(authorization: null);

        result.None.ShouldBeTrue();
    }

    [Fact]
    public async Task Request_with_another_scheme_is_left_to_the_authorization_policy()
    {
        var result = await AuthenticateAsync($"Basic {OrganizationKey}");

        result.None.ShouldBeTrue();
    }

    [Fact]
    public async Task Bearer_without_a_key_fails()
    {
        var result = await AuthenticateAsync("Bearer");

        result.Failure.ShouldNotBeNull().Message.ShouldBe("The API key is missing.");
    }

    [Fact]
    public async Task Invalid_key_fails()
    {
        var result = await AuthenticateAsync("Bearer wrong-key");

        result.Failure.ShouldNotBeNull().Message.ShouldBe("The API key is not valid.");
    }

    [Theory]
    [InlineData("Bearer")]
    [InlineData("bearer")]
    public async Task Organization_key_signs_in_the_organization(string scheme)
    {
        var result = await AuthenticateAsync($"{scheme} {OrganizationKey}");

        result.Succeeded.ShouldBeTrue();
        result.Principal.IsInRole(CallerClaims.OrganizationRole).ShouldBeTrue();
        result.Principal.FindFirst(CallerClaims.ConsumerIdType).ShouldBeNull();
    }

    [Fact]
    public async Task Consumer_key_signs_in_its_consumer()
    {
        var consumerId = Guid.CreateVersion7(Now);
        var (key, plainTextKey) = ConsumerKey.Issue(consumerId, Now);

        var result = await AuthenticateAsync($"Bearer {plainTextKey}", key);

        result.Succeeded.ShouldBeTrue();
        result.Principal.IsInRole(CallerClaims.ConsumerRole).ShouldBeTrue();
        result.Principal.FindFirstValue(CallerClaims.ConsumerIdType).ShouldBe(consumerId.ToString());
    }

    [Fact]
    public async Task Challenge_asks_for_a_bearer_key()
    {
        var (handler, context) = await CreateHandlerAsync(authorization: null);

        await handler.ChallengeAsync(properties: null);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
        context.Response.Headers.WWWAuthenticate.ToString().ShouldBe("Bearer");
    }

    private static async Task<AuthenticateResult> AuthenticateAsync(string? authorization, params ConsumerKey[] storedKeys)
    {
        var (handler, _) = await CreateHandlerAsync(authorization, storedKeys);

        return await handler.AuthenticateAsync();
    }

    private static async Task<(BearerKeyAuthenticationHandler Handler, HttpContext Context)> CreateHandlerAsync(
        string? authorization,
        params ConsumerKey[] storedKeys)
    {
        var context = new DefaultHttpContext();

        if (authorization is not null)
        {
            context.Request.Headers.Authorization = authorization;
        }

        var authenticator = new KeyAuthenticator(
            Options.Create(new AuthOptions { OrganizationKey = OrganizationKey }),
            new FakeConsumerKeyRepository(storedKeys));

        var handler = new BearerKeyAuthenticationHandler(
            new SchemeOptionsMonitor(),
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            authenticator);

        var scheme = new AuthenticationScheme(
            BearerKeyAuthenticationHandler.SchemeName,
            displayName: null,
            typeof(BearerKeyAuthenticationHandler));

        await handler.InitializeAsync(scheme, context);

        return (handler, context);
    }

    private sealed class SchemeOptionsMonitor : IOptionsMonitor<AuthenticationSchemeOptions>
    {
        public AuthenticationSchemeOptions CurrentValue { get; } = new();

        public AuthenticationSchemeOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<AuthenticationSchemeOptions, string?> listener) => null;
    }
}
