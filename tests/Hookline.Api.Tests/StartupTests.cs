using Microsoft.Extensions.Options;

using Shouldly;

using static Hookline.Api.Tests.HooklineApiFactory;

namespace Hookline.Api.Tests;

public class StartupTests
{
    [Fact]
    public void Startup_fails_when_the_connection_string_is_empty()
    {
        var exception = Should.Throw<InvalidOperationException>(() => StartWith(("ConnectionStrings:Hookline", "")));

        exception.Message.ShouldBe("Connection string 'Hookline' is not configured.");
    }

    [Fact]
    public void Startup_fails_when_the_organization_key_is_empty()
    {
        var exception = Should.Throw<OptionsValidationException>(() => StartWith(("Auth:OrganizationKey", "")));

        exception.Message.ShouldBe("'Auth:OrganizationKey' is not configured.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not base64!")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAA==")] // 16 bytes
    public void Startup_fails_when_the_encryption_key_is_invalid(string key)
    {
        var exception = Should.Throw<OptionsValidationException>(() => StartWith(("Encryption:Key", key)));

        exception.Message.ShouldBe("'Encryption:Key' must be a base64-encoded key of 32 bytes.");
    }

    [Fact]
    public void Startup_does_not_touch_the_database_when_migrations_are_off()
    {
        // The factory's database is unreachable, so applying migrations would make the startup fail.
        Should.NotThrow(() => StartWith(("Database:ApplyMigrationsOnStartup", "false")));
    }
}
