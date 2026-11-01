using System.Net;
using System.Net.Http.Json;
using System.Text;

using Hookline.Modules.Management.Application.Consumers;
using Hookline.Modules.Management.Tests.Infrastructure;

using Shouldly;

namespace Hookline.Modules.Management.Tests.Integration;

[Collection(PostgreSqlCollection.Name)]
public class ConsumerRoutesTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Organization_creates_a_consumer()
    {
        using var client = fixture.CreateClient();
        var externalId = UniqueExternalId();

        var response = await client.PostAsJsonAsync("/consumers", new { name = "  Loja 1  ", externalId });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var consumer = (await response.Content.ReadFromJsonAsync<ConsumerDto>())!;
        consumer.Name.ShouldBe("Loja 1");
        consumer.ExternalId.ShouldBe(externalId);
        consumer.CreatedAt.ShouldBe(fixture.Clock.GetUtcNow());
        response.Headers.Location.ShouldBe(new Uri($"/consumers/{consumer.Id}", UriKind.Relative));
    }

    [Fact]
    public async Task Create_rejects_an_invalid_consumer()
    {
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/consumers", new { name = " " });

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "consumer.name_required");
    }

    [Fact]
    public async Task Create_rejects_a_body_that_is_not_json()
    {
        using var client = fixture.CreateClient();

        var response = await client.PostAsync("/consumers", new StringContent("not json", Encoding.UTF8, "application/json"));

        // In production the framework answers 400 itself, without a code field (accepted in ADR 0003).
        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_rejects_an_external_id_already_in_use()
    {
        var existing = await fixture.CreateConsumerAsync(UniqueExternalId());
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/consumers", new { name = "Loja 2", externalId = existing.ExternalId });

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "consumer.external_id_taken");
    }

    [Fact]
    public async Task Create_requires_a_key()
    {
        using var client = fixture.CreateClient(key: null);

        var response = await client.PostAsJsonAsync("/consumers", new { name = "Loja 1" });

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Consumer_cannot_create_consumers()
    {
        using var client = await CreateConsumerClientAsync();

        var response = await client.PostAsJsonAsync("/consumers", new { name = "Loja 2" });

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Organization_lists_consumers()
    {
        var first = await fixture.CreateConsumerAsync();
        var second = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient();

        var consumers = (await client.GetFromJsonAsync<List<ConsumerDto>>("/consumers"))!;

        consumers.ShouldContain(first);
        consumers.ShouldContain(second);
    }

    [Fact]
    public async Task Consumer_cannot_list_consumers()
    {
        using var client = await CreateConsumerClientAsync();

        var response = await client.GetAsync("/consumers");

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Organization_gets_a_consumer()
    {
        var consumer = await fixture.CreateConsumerAsync(UniqueExternalId());
        using var client = fixture.CreateClient();

        (await client.GetFromJsonAsync<ConsumerDto>($"/consumers/{consumer.Id}")).ShouldBe(consumer);
    }

    [Fact]
    public async Task Consumer_gets_itself()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient(await fixture.IssueKeyAsync(consumer.Id));

        (await client.GetFromJsonAsync<ConsumerDto>($"/consumers/{consumer.Id}")).ShouldBe(consumer);
    }

    [Fact]
    public async Task Another_consumer_looks_exactly_like_a_missing_one()
    {
        var other = await fixture.CreateConsumerAsync();
        using var consumerClient = await CreateConsumerClientAsync();
        using var organizationClient = fixture.CreateClient();

        var forOtherConsumer = await (await consumerClient.GetAsync($"/consumers/{other.Id}"))
            .ShouldBeProblemAsync(HttpStatusCode.NotFound, "consumer.not_found");

        var forMissingConsumer = await (await organizationClient.GetAsync($"/consumers/{MissingId()}"))
            .ShouldBeProblemAsync(HttpStatusCode.NotFound, "consumer.not_found");

        forOtherConsumer.GetProperty("title").GetString().ShouldBe(forMissingConsumer.GetProperty("title").GetString());
        forOtherConsumer.GetProperty("detail").GetString().ShouldBe(forMissingConsumer.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Organization_deletes_a_consumer_and_its_keys_stop_working()
    {
        var consumer = await fixture.CreateConsumerAsync();
        var key = await fixture.IssueKeyAsync(consumer.Id);
        using var client = fixture.CreateClient();

        var response = await client.DeleteAsync($"/consumers/{consumer.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await (await client.GetAsync($"/consumers/{consumer.Id}")).ShouldBeProblemAsync(HttpStatusCode.NotFound);

        using var deletedConsumerClient = fixture.CreateClient(key);
        await (await deletedConsumerClient.GetAsync($"/consumers/{consumer.Id}")).ShouldBeProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_reports_a_missing_consumer()
    {
        using var client = fixture.CreateClient();

        var response = await client.DeleteAsync($"/consumers/{MissingId()}");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "consumer.not_found");
    }

    [Fact]
    public async Task Consumer_cannot_delete_itself()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient(await fixture.IssueKeyAsync(consumer.Id));

        var response = await client.DeleteAsync($"/consumers/{consumer.Id}");

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Consumer_route_with_an_invalid_id_does_not_exist()
    {
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/consumers/not-a-guid");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound);
    }

    // A client authenticated as a brand new consumer.
    private async Task<HttpClient> CreateConsumerClientAsync()
    {
        var consumer = await fixture.CreateConsumerAsync();

        return fixture.CreateClient(await fixture.IssueKeyAsync(consumer.Id));
    }

    // The database is shared by every test, so external ids must not repeat.
    private string UniqueExternalId() => Guid.CreateVersion7(fixture.Clock.GetUtcNow()).ToString("N");

    private Guid MissingId() => Guid.CreateVersion7(fixture.Clock.GetUtcNow());
}
