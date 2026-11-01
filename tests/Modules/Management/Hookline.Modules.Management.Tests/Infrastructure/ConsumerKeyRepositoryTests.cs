using Hookline.Modules.Management.Domain.ConsumerKeys;
using Hookline.Modules.Management.Domain.Consumers;
using Hookline.Modules.Management.Infrastructure.Repositories;

using Shouldly;

namespace Hookline.Modules.Management.Tests.Infrastructure;

[Collection(PostgreSqlCollection.Name)]
public class ConsumerKeyRepositoryTests(PostgreSqlFixture database)
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Key_is_found_by_its_hash()
    {
        var consumer = Consumer.Create("Loja 1", externalId: null, Now).Value;
        var (key, plainTextKey) = ConsumerKey.Issue(consumer.Id, Now);

        await using (var context = database.CreateContext())
        {
            context.AddRange(consumer, key);
            await context.SaveChangesAsync();
        }

        await using var readContext = database.CreateContext();
        var found = await new ConsumerKeyRepository(readContext)
            .FindByHashAsync(ConsumerKey.ComputeHash(plainTextKey), CancellationToken.None);

        found.ShouldNotBeNull();
        found.Id.ShouldBe(key.Id);
        found.ConsumerId.ShouldBe(consumer.Id);
    }

    [Fact]
    public async Task Unknown_hash_finds_nothing()
    {
        var (_, plainTextKey) = ConsumerKey.Issue(Guid.CreateVersion7(Now), Now);

        await using var context = database.CreateContext();
        var found = await new ConsumerKeyRepository(context)
            .FindByHashAsync(ConsumerKey.ComputeHash(plainTextKey), CancellationToken.None);

        found.ShouldBeNull();
    }
}
