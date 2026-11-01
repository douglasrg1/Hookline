using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hookline.Api.Tests;

// The pipeline around authentication. The key checks themselves are covered in the Management tests.
public class AuthenticationTests(HooklineApiFactory factory) : IClassFixture<HooklineApiFactory>
{
    // A route that does not exist: the fallback policy protects it too, and nothing here touches the database.
    private const string ProtectedPath = "/not-a-route";

    [Fact]
    public async Task Request_without_a_key_gets_a_bearer_challenge()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ProtectedPath);

        await ShouldBeUnauthorizedAsync(response);
    }

    [Fact]
    public async Task Request_with_a_wrong_key_is_rejected()
    {
        using var client = factory.CreateClientWithKey("wrong-key");

        var response = await client.GetAsync(ProtectedPath);

        await ShouldBeUnauthorizedAsync(response);
    }

    [Fact]
    public async Task Request_with_another_scheme_is_rejected()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Basic {HooklineApiFactory.OrganizationKey}");

        var response = await client.GetAsync(ProtectedPath);

        await ShouldBeUnauthorizedAsync(response);
    }

    [Fact]
    public async Task Request_with_the_organization_key_passes_authentication()
    {
        using var client = factory.CreateClientWithKey(HooklineApiFactory.OrganizationKey);

        var response = await client.GetAsync(ProtectedPath);

        // The route does not exist yet: getting past authentication means a 404.
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Health_is_public()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        // The test database is unreachable, so the check answers Unhealthy, but it answers.
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    // The fallback policy protects every route that does not opt out. This makes opting out visible:
    // a route marked as public by mistake fails here.
    [Fact]
    public void Only_health_is_public_in_production()
    {
        var publicRoutes = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(endpoint => endpoint.RoutePattern.RawText);

        publicRoutes.ShouldBe(["/health"]);
    }

    [Fact]
    public async Task Failure_while_checking_a_consumer_key_returns_a_generic_problem()
    {
        // Consumer keys are looked up in the database, which is unreachable in these tests.
        using var client = factory.CreateClientWithKey("hl_unknown-key");

        var response = await client.GetAsync(ProtectedPath);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().ShouldBe("internal_error");
    }

    private static async Task ShouldBeUnauthorizedAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ToString().ShouldBe("Bearer");
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/problem+json");

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("status").GetInt32().ShouldBe(401);
    }
}
