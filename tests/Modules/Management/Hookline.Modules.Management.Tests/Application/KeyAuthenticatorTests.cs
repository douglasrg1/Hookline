using Hookline.Modules.Management.Application.Authentication;
using Hookline.Modules.Management.Domain.ConsumerKeys;
using Hookline.Modules.Management.Tests.Fakes;

using Microsoft.Extensions.Options;

using Shouldly;

namespace Hookline.Modules.Management.Tests.Application;

public class KeyAuthenticatorTests
{
    private const string OrganizationKey = "organization-key";

    private static readonly DateTimeOffset Now = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid ConsumerId = Guid.CreateVersion7(Now);

    [Fact]
    public async Task Organization_key_authenticates_the_organization()
    {
        var caller = await AuthenticateAsync(OrganizationKey);

        caller.ShouldBe(Caller.Organization);
    }

    [Fact]
    public async Task Consumer_key_authenticates_its_consumer()
    {
        var (key, plainTextKey) = ConsumerKey.Issue(ConsumerId, Now);

        var caller = await AuthenticateAsync(plainTextKey, key);

        caller.ShouldBe(Caller.Consumer(ConsumerId));
    }

    [Fact]
    public async Task Revoked_consumer_key_is_rejected()
    {
        var (key, plainTextKey) = ConsumerKey.Issue(ConsumerId, Now);
        key.Revoke(Now);

        (await AuthenticateAsync(plainTextKey, key)).ShouldBeNull();
    }

    [Fact]
    public async Task Unknown_consumer_key_is_rejected()
    {
        var (_, plainTextKey) = ConsumerKey.Issue(ConsumerId, Now);

        (await AuthenticateAsync(plainTextKey)).ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("wrong-key")]
    [InlineData("ORGANIZATION-KEY")]
    [InlineData("organization-key ")]
    public async Task Other_keys_are_rejected_without_looking_up_the_database(string key)
    {
        var repository = new FakeConsumerKeyRepository();

        var caller = await new KeyAuthenticator(Settings(), repository).AuthenticateAsync(key, CancellationToken.None);

        caller.ShouldBeNull();
        repository.Lookups.ShouldBe(0);
    }

    private static Task<Caller?> AuthenticateAsync(string key, params ConsumerKey[] storedKeys) =>
        new KeyAuthenticator(Settings(), new FakeConsumerKeyRepository(storedKeys))
            .AuthenticateAsync(key, CancellationToken.None);

    private static IOptions<AuthOptions> Settings() =>
        Options.Create(new AuthOptions { OrganizationKey = OrganizationKey });
}
