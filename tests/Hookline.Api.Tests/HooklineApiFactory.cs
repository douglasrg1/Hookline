using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Hookline.Api.Tests;

public sealed class HooklineApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Production, so the tests see exactly what a real caller would see.
        builder.UseEnvironment("Production");

        // The modules need a connection string to start. These tests never open a connection.
        builder.UseSetting(
            "ConnectionStrings:Hookline",
            "Host=localhost;Database=hookline;Username=hookline;Password=not-used");
    }
}
