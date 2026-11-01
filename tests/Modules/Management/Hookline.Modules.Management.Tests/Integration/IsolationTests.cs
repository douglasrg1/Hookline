using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Hookline.Modules.Management.Application.ConsumerKeys;
using Hookline.Modules.Management.Application.Endpoints;
using Hookline.Modules.Management.Application.Pagination;
using Hookline.Modules.Management.Presentation.Authentication;
using Hookline.Modules.Management.Tests.Infrastructure;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

using Shouldly;

namespace Hookline.Modules.Management.Tests.Integration;

// These tests run against every route under /consumers/{consumerId}, read from the API itself,
// so a route added later is covered without touching this file. A route with a new kind of id
// makes them fail until they are told which value to use for it.
[Collection(PostgreSqlCollection.Name)]
public class IsolationTests(PostgreSqlFixture fixture)
{
    private const string Url = "https://isolation.example/webhooks";
    private const string OtherUrl = "https://isolation.example/other-webhooks";

    [Fact]
    public async Task Consumer_gets_nothing_from_the_routes_of_another_consumer()
    {
        var target = await CreateConsumerWithDataAsync();
        var intruder = await fixture.CreateConsumerAsync();
        using var intruderClient = fixture.CreateClient(await fixture.IssueKeyAsync(intruder.Id));
        var failures = new List<string>();

        foreach (var route in ConsumerRoutes())
        {
            using var response = await SendAsync(intruderClient, route, target);

            // Routes for the organization only answer 403 to every consumer, whatever the target,
            // so they reveal nothing. The others answer as if the target did not exist.
            var (status, code) = route.OrganizationOnly
                ? (HttpStatusCode.Forbidden, null)
                : (HttpStatusCode.NotFound, "consumer.not_found");

            if (response.StatusCode != status || await CodeOfAsync(response) != code)
            {
                failures.Add($"{route}: {(int)response.StatusCode} {await CodeOfAsync(response)}, expected {(int)status} {code}");
            }
        }

        failures.ShouldBeEmpty();
        await ShouldBeUntouchedAsync(target);
    }

    [Fact]
    public async Task Organization_reaches_every_route_of_a_consumer()
    {
        using var client = fixture.CreateClient();
        var failures = new List<string>();

        foreach (var route in ConsumerRoutes())
        {
            // Fresh data for every route, because some routes delete or change what they reach.
            var target = await CreateConsumerWithDataAsync();

            using var response = await SendAsync(client, route, target);

            if (!response.IsSuccessStatusCode)
            {
                failures.Add($"{route}: {(int)response.StatusCode} {await CodeOfAsync(response)}");
            }
        }

        failures.ShouldBeEmpty();
    }

    private List<ConsumerRoute> ConsumerRoutes()
    {
        var routes = fixture.Routes
            .Where(endpoint => endpoint.RoutePattern.RawText!.StartsWith("/consumers/{consumerId", StringComparison.Ordinal))
            .SelectMany(endpoint => endpoint.Metadata
                .GetRequiredMetadata<IHttpMethodMetadata>()
                .HttpMethods
                .Select(method => new ConsumerRoute(method, endpoint)))
            .ToList();

        routes.ShouldNotBeEmpty();

        return routes;
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, ConsumerRoute route, TargetConsumer target)
    {
        using var request = new HttpRequestMessage(new HttpMethod(route.Method), route.PathFor(target));

        // Every body these routes read accepts this one: a URL no other endpoint of the target uses.
        if (route.Method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(new { url = OtherUrl });
        }

        return await client.SendAsync(request);
    }

    private async Task<TargetConsumer> CreateConsumerWithDataAsync()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient();

        using var endpointResponse = await client.PostAsJsonAsync($"/consumers/{consumer.Id}/endpoints", new { url = Url });
        endpointResponse.EnsureSuccessStatusCode();

        using var keyResponse = await client.PostAsync($"/consumers/{consumer.Id}/keys", content: null);
        keyResponse.EnsureSuccessStatusCode();

        return new TargetConsumer(
            consumer.Id,
            (await endpointResponse.Content.ReadFromJsonAsync<EndpointDto>())!,
            (await keyResponse.Content.ReadFromJsonAsync<IssuedConsumerKeyDto>())!);
    }

    // Nothing the intruder sent was applied: same endpoint, same key, still working.
    private async Task ShouldBeUntouchedAsync(TargetConsumer target)
    {
        using var client = fixture.CreateClient();

        var endpoints = (await client.GetFromJsonAsync<Page<EndpointDto>>($"/consumers/{target.ConsumerId}/endpoints"))!.Items;
        endpoints.Count.ShouldBe(1);
        endpoints[0].Url.ShouldBe(Url);
        endpoints[0].IsEnabled.ShouldBeTrue();
        endpoints[0].UpdatedAt.ShouldBe(target.Endpoint.UpdatedAt);

        var keys = (await client.GetFromJsonAsync<Page<ConsumerKeyDto>>($"/consumers/{target.ConsumerId}/keys"))!.Items;
        keys.Count.ShouldBe(1);
        keys[0].RevokedAt.ShouldBeNull();

        using var targetClient = fixture.CreateClient(target.Key.Key);
        (await targetClient.GetAsync($"/consumers/{target.ConsumerId}")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task<string?> CodeOfAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();

        if (body.Length == 0)
        {
            return null;
        }

        using var problem = JsonDocument.Parse(body);

        return problem.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private sealed record TargetConsumer(Guid ConsumerId, EndpointDto Endpoint, IssuedConsumerKeyDto Key);

    private sealed record ConsumerRoute(string Method, RouteEndpoint Endpoint)
    {
        public bool OrganizationOnly => Endpoint.Metadata
            .GetOrderedMetadata<IAuthorizeData>()
            .Any(data => data.Policy == AuthorizationPolicies.Organization);

        // The route pattern with every parameter replaced by the target's ids.
        public string PathFor(TargetConsumer target)
        {
            var values = new Dictionary<string, Guid>
            {
                ["consumerId"] = target.ConsumerId,
                ["endpointId"] = target.Endpoint.Id,
                ["keyId"] = target.Key.Id,
            };

            var segments = Endpoint.RoutePattern.PathSegments.Select(segment => string.Concat(segment.Parts.Select(part => part switch
            {
                RoutePatternLiteralPart literal => literal.Content,
                RoutePatternParameterPart parameter when values.TryGetValue(parameter.Name, out var value) => value.ToString(),
                RoutePatternParameterPart parameter => throw new InvalidOperationException(
                    $"The isolation tests have no value for the route parameter '{parameter.Name}' of {this}."),
                _ => throw new InvalidOperationException($"Unexpected route part in {this}."),
            })));

            return "/" + string.Join('/', segments);
        }

        public override string ToString() => $"{Method} {Endpoint.RoutePattern.RawText}";
    }
}
