using Hookline.Modules.Management.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Hookline.Modules.Management.Tests.Infrastructure;

// One PostgreSQL container shared by every test in the collection, with the migrations applied once.
// The database is never cleaned: each test creates its own data.
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17").Build();

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    internal ManagementDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ManagementDbContext>();
        ManagementDbContext.Configure(options, container.GetConnectionString());

        return new ManagementDbContext(options.Options);
    }
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL";
}
