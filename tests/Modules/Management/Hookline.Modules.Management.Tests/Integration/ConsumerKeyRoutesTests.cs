using System.Net;
using System.Net.Http.Json;

using Hookline.Modules.Management.Application.ConsumerKeys;
using Hookline.Modules.Management.Application.Pagination;
using Hookline.Modules.Management.Tests.Infrastructure;

using Shouldly;

namespace Hookline.Modules.Management.Tests.Integration;

[Collection(PostgreSqlCollection.Name)]
public class ConsumerKeyRoutesTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Organization_issues_a_key_that_authenticates_its_consumer()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient();

        var response = await client.PostAsync($"/consumers/{consumer.Id}/keys", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();

        var issued = (await response.Content.ReadFromJsonAsync<IssuedConsumerKeyDto>())!;
        issued.ConsumerId.ShouldBe(consumer.Id);
        issued.Key.ShouldStartWith("hl_");
        issued.DisplayPrefix.ShouldBe(issued.Key[..12]);
        issued.CreatedAt.ShouldBe(fixture.Clock.GetUtcNow());

        using var consumerClient = fixture.CreateClient(issued.Key);
        (await consumerClient.GetAsync($"/consumers/{consumer.Id}")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Issue_reports_a_missing_consumer()
    {
        using var client = fixture.CreateClient();

        var response = await client.PostAsync($"/consumers/{MissingId()}/keys", content: null);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "consumer.not_found");
    }

    [Fact]
    public async Task Organization_lists_keys_page_by_page_without_the_keys_themselves()
    {
        var consumer = await fixture.CreateConsumerAsync();
        await fixture.IssueKeyAsync(consumer.Id);
        await fixture.IssueKeyAsync(consumer.Id);
        using var client = fixture.CreateClient();

        var firstResponse = await client.GetAsync($"/consumers/{consumer.Id}/keys?limit=1");
        var first = (await firstResponse.Content.ReadFromJsonAsync<Page<ConsumerKeyDto>>())!;
        var second = (await client.GetFromJsonAsync<Page<ConsumerKeyDto>>($"/consumers/{consumer.Id}/keys?limit=1&after={first.Next}"))!;

        first.Items.Count.ShouldBe(1);
        first.Next.ShouldNotBeNull();
        second.Items.Count.ShouldBe(1);
        second.Next.ShouldBeNull();
        second.Items[0].Id.ShouldBeGreaterThan(first.Items[0].Id);
        first.Items.Concat(second.Items).ShouldAllBe(key => key.ConsumerId == consumer.Id && key.RevokedAt == null);

        // Only the display prefix: the plain text key is never returned again.
        (await firstResponse.Content.ReadAsStringAsync()).ShouldNotContain("\"key\"");
    }

    [Fact]
    public async Task List_reports_a_missing_consumer()
    {
        using var client = fixture.CreateClient();

        var response = await client.GetAsync($"/consumers/{MissingId()}/keys");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "consumer.not_found");
    }

    [Fact]
    public async Task Organization_revokes_a_key_and_it_stops_working()
    {
        var consumer = await fixture.CreateConsumerAsync();
        var key = await fixture.IssueKeyAsync(consumer.Id);
        using var client = fixture.CreateClient();
        var keyId = (await ListKeysAsync(client, consumer.Id)).Single().Id;

        var response = await client.DeleteAsync($"/consumers/{consumer.Id}/keys/{keyId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ListKeysAsync(client, consumer.Id)).Single().RevokedAt.ShouldBe(fixture.Clock.GetUtcNow());

        using var revokedClient = fixture.CreateClient(key);
        await (await revokedClient.GetAsync($"/consumers/{consumer.Id}")).ShouldBeProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Revoking_twice_keeps_the_first_revocation()
    {
        var consumer = await fixture.CreateConsumerAsync();
        await fixture.IssueKeyAsync(consumer.Id);
        using var client = fixture.CreateClient();
        var keyId = (await ListKeysAsync(client, consumer.Id)).Single().Id;
        var firstRevocation = fixture.Clock.GetUtcNow();

        await client.DeleteAsync($"/consumers/{consumer.Id}/keys/{keyId}");
        fixture.Clock.Advance(TimeSpan.FromMinutes(1));
        var response = await client.DeleteAsync($"/consumers/{consumer.Id}/keys/{keyId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ListKeysAsync(client, consumer.Id)).Single().RevokedAt.ShouldBe(firstRevocation);
    }

    [Fact]
    public async Task Revoke_reports_a_missing_key()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient();

        var response = await client.DeleteAsync($"/consumers/{consumer.Id}/keys/{MissingId()}");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "consumer_key.not_found");
    }

    [Fact]
    public async Task Key_cannot_be_revoked_through_another_consumer()
    {
        var owner = await fixture.CreateConsumerAsync();
        var other = await fixture.CreateConsumerAsync();
        var key = await fixture.IssueKeyAsync(owner.Id);
        using var client = fixture.CreateClient();
        var keyId = (await ListKeysAsync(client, owner.Id)).Single().Id;

        var response = await client.DeleteAsync($"/consumers/{other.Id}/keys/{keyId}");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "consumer_key.not_found");

        using var ownerClient = fixture.CreateClient(key);
        (await ownerClient.GetAsync($"/consumers/{owner.Id}")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Consumer_cannot_manage_its_own_keys()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient(await fixture.IssueKeyAsync(consumer.Id));

        await (await client.PostAsync($"/consumers/{consumer.Id}/keys", content: null)).ShouldBeProblemAsync(HttpStatusCode.Forbidden);
        await (await client.GetAsync($"/consumers/{consumer.Id}/keys")).ShouldBeProblemAsync(HttpStatusCode.Forbidden);
        await (await client.DeleteAsync($"/consumers/{consumer.Id}/keys/{MissingId()}")).ShouldBeProblemAsync(HttpStatusCode.Forbidden);
    }

    private static async Task<IReadOnlyList<ConsumerKeyDto>> ListKeysAsync(HttpClient client, Guid consumerId) =>
        (await client.GetFromJsonAsync<Page<ConsumerKeyDto>>($"/consumers/{consumerId}/keys"))!.Items;

    private Guid MissingId() => Guid.CreateVersion7(fixture.Clock.GetUtcNow());
}
