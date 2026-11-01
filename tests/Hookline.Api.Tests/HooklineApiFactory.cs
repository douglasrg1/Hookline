using System.Net.Http.Headers;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Hookline.Api.Tests;

public sealed class HooklineApiFactory : WebApplicationFactory<Program>
{
    public const string OrganizationKey = "test-organization-key";

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

    // A client that sends the given key as "Authorization: Bearer <key>" on every request.
    public HttpClient CreateClientWithKey(string key)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

        return client;
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
        builder.UseSetting("Auth:OrganizationKey", OrganizationKey);
        builder.UseSetting("Encryption:Key", Convert.ToBase64String(new byte[32]));
    }
}
