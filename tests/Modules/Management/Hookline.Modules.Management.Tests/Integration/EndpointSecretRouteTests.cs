using System.Net;
using System.Net.Http.Json;
using System.Text;

using Hookline.Modules.Management.Application.Endpoints;
using Hookline.Modules.Management.Domain.Endpoints;
using Hookline.Modules.Management.Infrastructure.Security;
using Hookline.Modules.Management.Tests.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hookline.Modules.Management.Tests.Integration;

[Collection(PostgreSqlCollection.Name)]
public class EndpointSecretRouteTests(PostgreSqlFixture fixture)
{
    private const string Url = "https://loja1.com/webhooks";

    [Fact]
    public async Task Organization_gets_the_signing_secret_without_it_being_cached()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient();
        var endpoint = await CreateEndpointAsync(client, consumer.Id);

        var response = await client.GetAsync($"/consumers/{consumer.Id}/endpoints/{endpoint.Id}/secret");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();

        var secret = (await response.Content.ReadFromJsonAsync<EndpointSecretDto>())!.Secret;
        secret.ShouldStartWith(EndpointSecret.Prefix);
        Convert.FromBase64String(secret[EndpointSecret.Prefix.Length..]).Length.ShouldBe(EndpointSecret.RandomByteLength);
    }

    [Fact]
    public async Task Secret_stays_the_same_between_reads()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient();
        var endpoint = await CreateEndpointAsync(client, consumer.Id);
        var path = $"/consumers/{consumer.Id}/endpoints/{endpoint.Id}/secret";

        var first = (await client.GetFromJsonAsync<EndpointSecretDto>(path))!.Secret;
        var second = (await client.GetFromJsonAsync<EndpointSecretDto>(path))!.Secret;

        second.ShouldBe(first);
    }

    [Fact]
    public async Task Secret_is_stored_encrypted()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient();
        var endpoint = await CreateEndpointAsync(client, consumer.Id);
        var secret = (await client.GetFromJsonAsync<EndpointSecretDto>(
            $"/consumers/{consumer.Id}/endpoints/{endpoint.Id}/secret"))!.Secret;

        await using var context = fixture.CreateContext();
        var stored = await context.Endpoints.Where(e => e.Id == endpoint.Id).Select(e => e.EncryptedSecret).SingleAsync();

        // The plain text never reaches the database, and only the master key turns the stored bytes back into it.
        Encoding.UTF8.GetString(stored).ShouldNotContain(secret[EndpointSecret.Prefix.Length..]);
        var protector = new AesGcmSecretProtector(Options.Create(new EncryptionOptions { Key = ManagementApiFactory.EncryptionKey }));
        protector.Unprotect(stored).ShouldBe(secret);
    }

    [Fact]
    public async Task Consumer_gets_the_secret_of_its_own_endpoint()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient(await fixture.IssueKeyAsync(consumer.Id));
        var endpoint = await CreateEndpointAsync(client, consumer.Id);

        var response = await client.GetAsync($"/consumers/{consumer.Id}/endpoints/{endpoint.Id}/secret");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Consumer_cannot_get_the_secret_of_another_consumer()
    {
        var owner = await fixture.CreateConsumerAsync();
        var other = await fixture.CreateConsumerAsync();
        using var organizationClient = fixture.CreateClient();
        var endpoint = await CreateEndpointAsync(organizationClient, owner.Id);
        using var otherClient = fixture.CreateClient(await fixture.IssueKeyAsync(other.Id));

        var response = await otherClient.GetAsync($"/consumers/{owner.Id}/endpoints/{endpoint.Id}/secret");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "consumer.not_found");
    }

    [Fact]
    public async Task Secret_cannot_be_reached_through_another_consumer()
    {
        var owner = await fixture.CreateConsumerAsync();
        var other = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient();
        var endpoint = await CreateEndpointAsync(client, owner.Id);

        var response = await client.GetAsync($"/consumers/{other.Id}/endpoints/{endpoint.Id}/secret");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "endpoint.not_found");
    }

    [Fact]
    public async Task Secret_reports_a_missing_endpoint()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient();

        var response = await client.GetAsync($"/consumers/{consumer.Id}/endpoints/{MissingId()}/secret");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "endpoint.not_found");
    }

    private static async Task<EndpointDto> CreateEndpointAsync(HttpClient client, Guid consumerId)
    {
        var response = await client.PostAsJsonAsync($"/consumers/{consumerId}/endpoints", new { url = Url });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<EndpointDto>())!;
    }

    private Guid MissingId() => Guid.CreateVersion7(fixture.Clock.GetUtcNow());
}
