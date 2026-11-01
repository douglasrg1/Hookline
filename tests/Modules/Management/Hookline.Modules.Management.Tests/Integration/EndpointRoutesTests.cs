using System.Net;
using System.Net.Http.Json;

using Hookline.Modules.Management.Application.Endpoints;
using Hookline.Modules.Management.Application.Pagination;
using Hookline.Modules.Management.Tests.Infrastructure;

using Shouldly;

namespace Hookline.Modules.Management.Tests.Integration;

[Collection(PostgreSqlCollection.Name)]
public class EndpointRoutesTests(PostgreSqlFixture fixture)
{
    private const string Url = "https://loja1.com/webhooks";
    private const string OtherUrl = "https://loja1.com/other-webhooks";

    [Fact]
    public async Task Organization_creates_an_endpoint()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/consumers/{consumer.Id}/endpoints",
            new { url = Url, eventTypes = new[] { "invoice.paid", "invoice.overdue" } });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var endpoint = (await response.Content.ReadFromJsonAsync<EndpointDto>())!;
        endpoint.ConsumerId.ShouldBe(consumer.Id);
        endpoint.Url.ShouldBe(Url);
        endpoint.EventTypes.ShouldBe(["invoice.paid", "invoice.overdue"]);
        endpoint.IsEnabled.ShouldBeTrue();
        endpoint.CreatedAt.ShouldBe(fixture.Clock.GetUtcNow());
        endpoint.UpdatedAt.ShouldBe(endpoint.CreatedAt);
        response.Headers.Location.ShouldBe(new Uri($"/consumers/{consumer.Id}/endpoints/{endpoint.Id}", UriKind.Relative));
    }

    [Fact]
    public async Task Consumer_creates_its_own_endpoint()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient(await fixture.IssueKeyAsync(consumer.Id));

        var response = await client.PostAsJsonAsync($"/consumers/{consumer.Id}/endpoints", new { url = Url });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Endpoint_without_event_types_receives_every_event()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient();

        var endpoint = await CreateEndpointAsync(client, consumer.Id, Url);

        endpoint.EventTypes.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("http://loja1.com/webhooks", null, "endpoint.https_required")]
    [InlineData("not a url", null, "endpoint.url_invalid")]
    [InlineData(Url, "Invoice.Paid", "endpoint.event_type_invalid")]
    public async Task Create_rejects_an_invalid_endpoint(string url, string? eventType, string code)
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient();
        string[] eventTypes = eventType is null ? [] : [eventType];

        var response = await client.PostAsJsonAsync($"/consumers/{consumer.Id}/endpoints", new { url, eventTypes });

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, code);
    }

    [Fact]
    public async Task Create_rejects_a_url_the_consumer_already_uses()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient();
        await CreateEndpointAsync(client, consumer.Id, Url);

        var response = await client.PostAsJsonAsync($"/consumers/{consumer.Id}/endpoints", new { url = Url });

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "endpoint.url_taken");
    }

    [Fact]
    public async Task Different_consumers_can_use_the_same_url()
    {
        var first = await fixture.CreateConsumerAsync();
        var second = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient();
        await CreateEndpointAsync(client, first.Id, Url);

        var response = await client.PostAsJsonAsync($"/consumers/{second.Id}/endpoints", new { url = Url });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_reports_a_missing_consumer()
    {
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync($"/consumers/{MissingId()}/endpoints", new { url = Url });

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "consumer.not_found");
    }

    [Fact]
    public async Task Consumer_cannot_create_endpoints_for_another_consumer()
    {
        var consumer = await fixture.CreateConsumerAsync();
        var other = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient(await fixture.IssueKeyAsync(consumer.Id));

        var response = await client.PostAsJsonAsync($"/consumers/{other.Id}/endpoints", new { url = Url });

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "consumer.not_found");
    }

    [Fact]
    public async Task Lists_only_the_consumer_endpoints_page_by_page()
    {
        var consumer = await fixture.CreateConsumerAsync();
        var other = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient();
        var first = await CreateEndpointAsync(client, consumer.Id, Url);
        var second = await CreateEndpointAsync(client, consumer.Id, OtherUrl);
        await CreateEndpointAsync(client, other.Id, Url);

        var firstPage = (await client.GetFromJsonAsync<Page<EndpointDto>>($"/consumers/{consumer.Id}/endpoints?limit=1"))!;
        var secondPage = (await client.GetFromJsonAsync<Page<EndpointDto>>(
            $"/consumers/{consumer.Id}/endpoints?limit=1&after={firstPage.Next}"))!;

        firstPage.Next.ShouldNotBeNull();
        secondPage.Next.ShouldBeNull();
        firstPage.Items.Concat(secondPage.Items).Select(endpoint => endpoint.Id).ShouldBe(
            new[] { first.Id, second.Id }.Order(),
            ignoreOrder: false);
    }

    [Fact]
    public async Task List_reports_a_missing_consumer()
    {
        using var client = fixture.CreateClient();

        var response = await client.GetAsync($"/consumers/{MissingId()}/endpoints");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "consumer.not_found");
    }

    [Fact]
    public async Task Gets_an_endpoint()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient(await fixture.IssueKeyAsync(consumer.Id));
        var created = await CreateEndpointAsync(client, consumer.Id, Url, "invoice.paid");

        var endpoint = (await client.GetFromJsonAsync<EndpointDto>($"/consumers/{consumer.Id}/endpoints/{created.Id}"))!;

        endpoint.Id.ShouldBe(created.Id);
        endpoint.Url.ShouldBe(Url);
        endpoint.EventTypes.ShouldBe(["invoice.paid"]);
    }

    [Fact]
    public async Task Get_reports_a_missing_endpoint()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient();

        var response = await client.GetAsync($"/consumers/{consumer.Id}/endpoints/{MissingId()}");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "endpoint.not_found");
    }

    [Fact]
    public async Task Endpoint_cannot_be_reached_through_another_consumer()
    {
        var owner = await fixture.CreateConsumerAsync();
        var other = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient();
        var endpoint = await CreateEndpointAsync(client, owner.Id, Url);
        var throughOther = $"/consumers/{other.Id}/endpoints/{endpoint.Id}";

        await (await client.GetAsync(throughOther)).ShouldBeProblemAsync(HttpStatusCode.NotFound, "endpoint.not_found");
        await (await client.PutAsJsonAsync(throughOther, new { url = OtherUrl })).ShouldBeProblemAsync(HttpStatusCode.NotFound, "endpoint.not_found");
        await (await client.PostAsync($"{throughOther}/disable", content: null)).ShouldBeProblemAsync(HttpStatusCode.NotFound, "endpoint.not_found");
        await (await client.DeleteAsync(throughOther)).ShouldBeProblemAsync(HttpStatusCode.NotFound, "endpoint.not_found");

        var unchanged = (await client.GetFromJsonAsync<EndpointDto>($"/consumers/{owner.Id}/endpoints/{endpoint.Id}"))!;
        unchanged.Url.ShouldBe(Url);
        unchanged.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public async Task Updates_an_endpoint()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient();
        var created = await CreateEndpointAsync(client, consumer.Id, Url, "invoice.paid");
        fixture.Clock.Advance(TimeSpan.FromMinutes(1));

        var response = await client.PutAsJsonAsync(
            $"/consumers/{consumer.Id}/endpoints/{created.Id}",
            new { url = OtherUrl, eventTypes = new[] { "invoice.overdue" } });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var updated = (await response.Content.ReadFromJsonAsync<EndpointDto>())!;
        updated.Url.ShouldBe(OtherUrl);
        updated.EventTypes.ShouldBe(["invoice.overdue"]);
        updated.CreatedAt.ShouldBe(created.CreatedAt);
        updated.UpdatedAt.ShouldBe(fixture.Clock.GetUtcNow());

        var stored = (await client.GetFromJsonAsync<EndpointDto>($"/consumers/{consumer.Id}/endpoints/{created.Id}"))!;
        stored.Url.ShouldBe(OtherUrl);
    }

    [Fact]
    public async Task Update_keeps_its_own_url()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient();
        var created = await CreateEndpointAsync(client, consumer.Id, Url);

        var response = await client.PutAsJsonAsync(
            $"/consumers/{consumer.Id}/endpoints/{created.Id}",
            new { url = Url, eventTypes = new[] { "invoice.paid" } });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_rejects_a_url_of_another_endpoint_of_the_consumer()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient();
        await CreateEndpointAsync(client, consumer.Id, Url);
        var second = await CreateEndpointAsync(client, consumer.Id, OtherUrl);

        var response = await client.PutAsJsonAsync($"/consumers/{consumer.Id}/endpoints/{second.Id}", new { url = Url });

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "endpoint.url_taken");
        (await client.GetFromJsonAsync<EndpointDto>($"/consumers/{consumer.Id}/endpoints/{second.Id}"))!.Url.ShouldBe(OtherUrl);
    }

    [Fact]
    public async Task Update_rejects_an_invalid_endpoint()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient();
        var created = await CreateEndpointAsync(client, consumer.Id, Url);

        var response = await client.PutAsJsonAsync(
            $"/consumers/{consumer.Id}/endpoints/{created.Id}",
            new { url = "http://loja1.com/webhooks" });

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "endpoint.https_required");
    }

    [Fact]
    public async Task Disables_and_enables_an_endpoint()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient(await fixture.IssueKeyAsync(consumer.Id));
        var created = await CreateEndpointAsync(client, consumer.Id, Url);
        var path = $"/consumers/{consumer.Id}/endpoints/{created.Id}";

        var disabled = await (await client.PostAsync($"{path}/disable", content: null)).Content.ReadFromJsonAsync<EndpointDto>();
        var enabled = await (await client.PostAsync($"{path}/enable", content: null)).Content.ReadFromJsonAsync<EndpointDto>();

        disabled!.IsEnabled.ShouldBeFalse();
        enabled!.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public async Task Deletes_an_endpoint()
    {
        var consumer = await fixture.CreateConsumerAsync();
        using var client = fixture.CreateClient();
        var created = await CreateEndpointAsync(client, consumer.Id, Url);
        var path = $"/consumers/{consumer.Id}/endpoints/{created.Id}";

        (await client.DeleteAsync(path)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await (await client.GetAsync(path)).ShouldBeProblemAsync(HttpStatusCode.NotFound, "endpoint.not_found");
        await (await client.DeleteAsync(path)).ShouldBeProblemAsync(HttpStatusCode.NotFound, "endpoint.not_found");
    }

    private static async Task<EndpointDto> CreateEndpointAsync(
        HttpClient client,
        Guid consumerId,
        string url,
        params string[] eventTypes)
    {
        var response = await client.PostAsJsonAsync($"/consumers/{consumerId}/endpoints", new { url, eventTypes });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<EndpointDto>())!;
    }

    private Guid MissingId() => Guid.CreateVersion7(fixture.Clock.GetUtcNow());
}
