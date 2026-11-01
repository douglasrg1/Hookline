using Microsoft.Extensions.Options;

using Npgsql;

using Shouldly;

using Testcontainers.PostgreSql;

using static Hookline.Api.Tests.HooklineApiFactory;

namespace Hookline.Api.Tests;

public class MigrationsOnStartupTests(MigrationsOnStartupTests.PostgreSqlServer server)
    : IClassFixture<MigrationsOnStartupTests.PostgreSqlServer>
{
    [Fact]
    public async Task Migrations_are_applied_when_enabled()
    {
        StartWith(
            ("ConnectionStrings:Hookline", server.ConnectionStringFor("applied")),
            ("Database:ApplyMigrationsOnStartup", "true"));

        var migrations = await server.QueryAsync("applied", "SELECT migration_id FROM management.\"__EFMigrationsHistory\"");

        migrations.ShouldContain(id => id.EndsWith("_InitialCreate"));
    }

    [Fact]
    public async Task Settings_are_checked_before_the_migrations_touch_the_database()
    {
        Should.Throw<OptionsValidationException>(() => StartWith(
            ("ConnectionStrings:Hookline", server.ConnectionStringFor("misconfigured")),
            ("Database:ApplyMigrationsOnStartup", "true"),
            ("Auth:OrganizationKey", "")));

        // Applying the migrations would have created the database.
        var databases = await server.QueryAsync("postgres", "SELECT datname FROM pg_database");

        databases.ShouldNotContain("misconfigured");
    }

    // One container for the class. Each test points the API to its own database,
    // which the migrations create when they run.
    public sealed class PostgreSqlServer : IAsyncLifetime
    {
        private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17").Build();

        public Task InitializeAsync() => container.StartAsync();

        public Task DisposeAsync() => container.DisposeAsync().AsTask();

        public string ConnectionStringFor(string database) =>
            new NpgsqlConnectionStringBuilder(container.GetConnectionString()) { Database = database }.ConnectionString;

        public async Task<List<string>> QueryAsync(string database, string sql)
        {
            await using var dataSource = NpgsqlDataSource.Create(ConnectionStringFor(database));
            await using var command = dataSource.CreateCommand(sql);
            await using var reader = await command.ExecuteReaderAsync();

            var values = new List<string>();

            while (await reader.ReadAsync())
            {
                values.Add(reader.GetString(0));
            }

            return values;
        }
    }
}
