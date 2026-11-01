using System.Net.Http.Headers;
using System.Net.Http.Json;

using Hookline.Modules.Management.Application.Consumers;
using Hookline.Modules.Management.Domain.ConsumerKeys;
using Hookline.Modules.Management.Infrastructure;
using Hookline.Modules.Management.Tests.Integration;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

using Testcontainers.PostgreSql;

namespace Hookline.Modules.Management.Tests.Infrastructure;

// One PostgreSQL container shared by every test in the collection, with the migrations applied once,
// and the API running on top of it. The database is never cleaned: each test creates its own data.
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17").Build();

    private ManagementApiFactory? api;

    // The API's clock. Whole seconds, because PostgreSQL keeps timestamps with microsecond precision.
    internal FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (api is not null)
        {
            await api.DisposeAsync();
        }

        await container.DisposeAsync();
    }

    internal ManagementDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ManagementDbContext>();
        ManagementDbContext.Configure(options, container.GetConnectionString());

        return new ManagementDbContext(options.Options);
    }

    // A client of the API, sending the given key as "Authorization: Bearer <key>", or no key at all.
    internal HttpClient CreateClient(string? key = ManagementApiFactory.OrganizationKey)
    {
        // Started on first use, so the tests that only touch the database do not pay for it.
        api ??= new ManagementApiFactory(container.GetConnectionString(), Clock);

        var client = api.CreateClient();

        if (key is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
        }

        return client;
    }

    // Creates a consumer through the API, as the organization.
    internal async Task<ConsumerDto> CreateConsumerAsync(string? externalId = null)
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/consumers", new { name = "Loja 1", externalId });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<ConsumerDto>())!;
    }

    // Issues a key straight in the database and returns it in plain text, ready to authenticate.
    internal async Task<string> IssueKeyAsync(Guid consumerId)
    {
        var (key, plainTextKey) = ConsumerKey.Issue(consumerId, Clock.GetUtcNow());

        await using var context = CreateContext();
        context.ConsumerKeys.Add(key);
        await context.SaveChangesAsync();

        return plainTextKey;
    }
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL";
}
