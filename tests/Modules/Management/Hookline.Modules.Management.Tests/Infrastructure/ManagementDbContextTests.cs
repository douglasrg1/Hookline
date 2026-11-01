using Hookline.Modules.Management.Domain.ConsumerKeys;
using Hookline.Modules.Management.Domain.Consumers;
using Hookline.Modules.Management.Domain.Endpoints;
using Microsoft.EntityFrameworkCore;
using Npgsql;

using Shouldly;

namespace Hookline.Modules.Management.Tests.Infrastructure;

[Collection(PostgreSqlCollection.Name)]
public class ManagementDbContextTests(PostgreSqlFixture database)
{
    private const string Url = "https://loja1.com/webhooks";

    // Whole seconds: PostgreSQL keeps timestamps with microsecond precision.
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);
    private static readonly byte[] Secret = [1, 2, 3];

    [Fact]
    public async Task Consumer_is_saved_and_read_back()
    {
        var consumer = NewConsumer(UniqueExternalId());
        await SaveAsync(consumer);

        await using var context = database.CreateContext();
        var stored = await context.Consumers.SingleAsync(c => c.Id == consumer.Id);

        stored.Name.ShouldBe(consumer.Name);
        stored.ExternalId.ShouldBe(consumer.ExternalId);
        stored.CreatedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task Consumer_key_is_found_by_the_hash_of_the_plain_text_key()
    {
        var consumer = NewConsumer();
        var (key, plainTextKey) = ConsumerKey.Issue(consumer.Id, Now);
        key.Revoke(Now.AddDays(1));
        await SaveAsync(consumer, key);

        await using var context = database.CreateContext();
        var hash = ConsumerKey.ComputeHash(plainTextKey);
        var stored = await context.ConsumerKeys.SingleAsync(k => k.Hash == hash);

        stored.Id.ShouldBe(key.Id);
        stored.ConsumerId.ShouldBe(consumer.Id);
        stored.DisplayPrefix.ShouldBe(key.DisplayPrefix);
        stored.CreatedAt.ShouldBe(Now);
        stored.RevokedAt.ShouldBe(Now.AddDays(1));
        stored.IsRevoked.ShouldBeTrue();
    }

    [Fact]
    public async Task Endpoint_is_saved_and_read_back()
    {
        var consumer = NewConsumer();
        var endpoint = NewEndpoint(consumer, "invoice.paid", "invoice.overdue");
        endpoint.Disable(Now.AddMinutes(1));
        await SaveAsync(consumer, endpoint);

        await using var context = database.CreateContext();
        var stored = await context.Endpoints.SingleAsync(e => e.Id == endpoint.Id);

        stored.ConsumerId.ShouldBe(consumer.Id);
        stored.Url.ShouldBe(Url);
        stored.EventTypes.ShouldBe(["invoice.paid", "invoice.overdue"]);
        stored.EncryptedSecret.ShouldBe(Secret);
        stored.IsEnabled.ShouldBeFalse();
        stored.CreatedAt.ShouldBe(Now);
        stored.UpdatedAt.ShouldBe(Now.AddMinutes(1));
    }

    [Fact]
    public async Task Endpoint_without_event_types_is_read_back_with_an_empty_list()
    {
        var consumer = NewConsumer();
        var endpoint = NewEndpoint(consumer);
        await SaveAsync(consumer, endpoint);

        await using var context = database.CreateContext();
        var stored = await context.Endpoints.SingleAsync(e => e.Id == endpoint.Id);

        stored.EventTypes.ShouldBeEmpty();
        stored.Accepts("invoice.paid").ShouldBeTrue();
    }

    [Fact]
    public async Task Consumers_cannot_share_an_external_id()
    {
        var externalId = UniqueExternalId();
        await SaveAsync(NewConsumer(externalId));

        var exception = await Should.ThrowAsync<DbUpdateException>(() => SaveAsync(NewConsumer(externalId)));

        ShouldViolateUniqueIndex(exception.InnerException, "ix_consumers_external_id");
    }

    [Fact]
    public async Task Consumers_without_an_external_id_do_not_conflict()
    {
        await Should.NotThrowAsync(() => SaveAsync(NewConsumer(), NewConsumer()));
    }

    [Fact]
    public async Task Consumer_keys_cannot_share_a_hash()
    {
        var consumer = NewConsumer();
        var (key, _) = ConsumerKey.Issue(consumer.Id, Now);
        await SaveAsync(consumer, key);

        // The domain cannot issue two keys with the same hash, so the copy is made in SQL.
        await using var context = database.CreateContext();
        var exception = await Should.ThrowAsync<PostgresException>(() => context.Database.ExecuteSqlAsync($"""
            INSERT INTO management.consumer_keys (id, consumer_id, hash, display_prefix, created_at)
            SELECT gen_random_uuid(), consumer_id, hash, display_prefix, created_at
            FROM management.consumer_keys
            WHERE id = {key.Id}
            """));

        ShouldViolateUniqueIndex(exception, "ix_consumer_keys_hash");
    }

    [Fact]
    public async Task Consumer_cannot_register_the_same_url_twice()
    {
        var consumer = NewConsumer();
        await SaveAsync(consumer, NewEndpoint(consumer));

        var exception = await Should.ThrowAsync<DbUpdateException>(() => SaveAsync(NewEndpoint(consumer)));

        ShouldViolateUniqueIndex(exception.InnerException, "ix_endpoints_consumer_id_url");
    }

    [Fact]
    public async Task Different_consumers_can_register_the_same_url()
    {
        var first = NewConsumer();
        var second = NewConsumer();

        await Should.NotThrowAsync(() => SaveAsync(first, second, NewEndpoint(first), NewEndpoint(second)));
    }

    [Fact]
    public async Task Deleting_a_consumer_deletes_its_keys_and_endpoints()
    {
        var consumer = NewConsumer();
        var (key, _) = ConsumerKey.Issue(consumer.Id, Now);
        await SaveAsync(consumer, key, NewEndpoint(consumer));

        // Deleted in SQL, so the cascade comes from the database and not from the change tracker.
        await using var context = database.CreateContext();
        await context.Consumers.Where(c => c.Id == consumer.Id).ExecuteDeleteAsync();

        (await context.ConsumerKeys.CountAsync(k => k.ConsumerId == consumer.Id)).ShouldBe(0);
        (await context.Endpoints.CountAsync(e => e.ConsumerId == consumer.Id)).ShouldBe(0);
    }

    [Fact]
    public async Task Tables_and_migrations_history_are_in_the_management_schema()
    {
        await using var context = database.CreateContext();

        var tables = await context.Database
            .SqlQuery<string>($"SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'management'")
            .ToListAsync();

        tables.ShouldBe(["__EFMigrationsHistory", "consumer_keys", "consumers", "endpoints"], ignoreOrder: true);
    }

    private static Consumer NewConsumer(string? externalId = null) =>
        Consumer.Create("Loja 1", externalId, Now).Value;

    private static Endpoint NewEndpoint(Consumer consumer, params string[] eventTypes) =>
        Endpoint.Create(consumer.Id, Url, eventTypes, Secret, Now).Value;

    // The database is shared by every test, so external ids must not repeat.
    private static string UniqueExternalId() => Guid.CreateVersion7(Now).ToString("N");

    private static void ShouldViolateUniqueIndex(Exception? exception, string indexName)
    {
        var postgresException = exception.ShouldBeOfType<PostgresException>();
        postgresException.SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
        postgresException.ConstraintName.ShouldBe(indexName);
    }

    private async Task SaveAsync(params object[] entities)
    {
        await using var context = database.CreateContext();
        context.AddRange(entities);
        await context.SaveChangesAsync();
    }
}
