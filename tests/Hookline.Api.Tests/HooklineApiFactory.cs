using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Hookline.Api.Tests;

public sealed class HooklineApiFactory : WebApplicationFactory<Program>
{
    // Starts the API with the factory's valid settings, overriding the given ones, and stops it.
    public static void StartWith(params (string Key, string Value)[] settings)
    {
        using var baseFactory = new HooklineApiFactory();
        using var factory = baseFactory.WithWebHostBuilder(builder =>
        {
            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }
        });

        using var client = factory.CreateClient();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Production, so the tests see exactly what a real caller would see.
        builder.UseEnvironment("Production");

        // The modules need a connection string to start. Nothing listens on port 1, so an unexpected
        // database access fails the test instead of reaching a database on the developer's machine.
        builder.UseSetting(
            "ConnectionStrings:Hookline",
            "Host=localhost;Port=1;Database=hookline;Username=hookline;Password=not-used");

        // Explicit, so a local appsettings.Development.json cannot turn migrations on.
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");

        // Valid values for the required settings. Any 32 bytes make a valid encryption key.
        builder.UseSetting("Auth:OrganizationKey", "test-organization-key");
        builder.UseSetting("Encryption:Key", Convert.ToBase64String(new byte[32]));
    }
}
