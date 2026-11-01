using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using Shouldly;

namespace Hookline.Api.Tests;

public class ErrorHandlingTests(HooklineApiFactory factory) : IClassFixture<HooklineApiFactory>
{
    private const string ProblemJson = "application/problem+json";
    private const string SensitiveMessage = "connection to 10.0.0.12 failed for user admin";

    [Fact]
    public async Task Unhandled_exception_returns_a_generic_problem_without_internal_details()
    {
        using var client = CreateClientThrowing(new InvalidOperationException(SensitiveMessage));

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemJson);

        using var problem = JsonDocument.Parse(body);
        problem.RootElement.GetProperty("status").GetInt32().ShouldBe(500);
        problem.RootElement.GetProperty("title").GetString().ShouldBe("An unexpected error occurred.");
        problem.RootElement.GetProperty("code").GetString().ShouldBe("internal_error");
        problem.RootElement.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();

        body.ShouldNotContain(SensitiveMessage);
        body.ShouldNotContain(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task Bad_request_exception_keeps_its_status_code_without_internal_details()
    {
        using var client = CreateClientThrowing(
            new BadHttpRequestException(SensitiveMessage, StatusCodes.Status400BadRequest));

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemJson);

        using var problem = JsonDocument.Parse(body);
        problem.RootElement.GetProperty("status").GetInt32().ShouldBe(400);
        problem.RootElement.GetProperty("title").GetString().ShouldBe("Bad Request");
        problem.RootElement.GetProperty("code").GetString().ShouldBe("bad_request");

        body.ShouldNotContain(SensitiveMessage);
    }

    [Fact]
    public async Task Unknown_route_returns_a_not_found_problem()
    {
        // Without a key, every route answers 401, existing or not.
        using var client = factory.CreateClientWithKey(HooklineApiFactory.OrganizationKey);

        var response = await client.GetAsync("/does-not-exist");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemJson);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("status").GetInt32().ShouldBe(404);
    }

    // The health endpoint runs inside the real pipeline. Making it throw proves that
    // an exception raised by any endpoint goes through the registered exception handlers.
    private HttpClient CreateClientThrowing(Exception exception) =>
        factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddSingleton<HealthCheckService>(new ThrowingHealthCheckService(exception))))
            .CreateClient();
}
