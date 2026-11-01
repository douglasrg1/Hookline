using Microsoft.AspNetCore.Hosting;

using Shouldly;

namespace Hookline.Api.Tests;

public class StartupTests
{
    [Fact]
    public void Startup_fails_when_the_connection_string_is_empty()
    {
        using var baseFactory = new HooklineApiFactory();
        using var factory = baseFactory.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:Hookline", string.Empty));

        var exception = Should.Throw<InvalidOperationException>(() => factory.CreateClient());

        exception.Message.ShouldBe("Connection string 'Hookline' is not configured.");
    }
}
