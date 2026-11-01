using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Hosting;

using Shouldly;

namespace Hookline.Api.Tests;

public class ApiReferenceTests(HooklineApiFactory factory) : IClassFixture<HooklineApiFactory>
{
    private const string OpenApiDocumentPath = "/openapi/v1.json";
    private const string ScalarPath = "/scalar";

    [Fact]
    public async Task Openapi_document_is_published_in_development()
    {
        using var client = CreateDevelopmentClient();

        var response = await client.GetAsync(OpenApiDocumentPath);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("openapi").GetString().ShouldStartWith("3.");
    }

    [Fact]
    public async Task Scalar_reference_page_is_served_in_development()
    {
        using var client = CreateDevelopmentClient();

        var response = await client.GetAsync(ScalarPath);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");
    }

    [Theory]
    [InlineData(OpenApiDocumentPath)]
    [InlineData(ScalarPath)]
    public async Task Api_reference_is_not_exposed_in_production(string path)
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private HttpClient CreateDevelopmentClient() =>
        factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"))
            .CreateClient();
}
