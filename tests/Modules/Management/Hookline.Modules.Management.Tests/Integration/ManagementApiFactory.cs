using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Hookline.Modules.Management.Tests.Integration;

// The whole API in memory, on top of the test database, with a clock the tests control.
internal sealed class ManagementApiFactory(string connectionString, TimeProvider clock) : WebApplicationFactory<Program>
{
    public const string OrganizationKey = "test-organization-key";

    // Any 32 bytes make a valid master key. Tests use it to read what the API stored.
    public static readonly string EncryptionKey = Convert.ToBase64String(new byte[32]);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Production, so the tests see exactly what a real caller would see.
        builder.UseEnvironment("Production");

        builder.UseSetting("ConnectionStrings:Hookline", connectionString);

        // The fixture has already applied the migrations.
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");

        builder.UseSetting("Auth:OrganizationKey", OrganizationKey);
        builder.UseSetting("Encryption:Key", EncryptionKey);

        builder.ConfigureTestServices(services => services.AddSingleton(clock));
    }
}
