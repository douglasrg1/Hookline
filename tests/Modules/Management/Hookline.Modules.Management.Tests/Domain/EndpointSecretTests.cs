using Hookline.Modules.Management.Domain.Endpoints;

using Shouldly;

namespace Hookline.Modules.Management.Tests.Domain;

public class EndpointSecretTests
{
    [Fact]
    public void Generated_secret_has_the_standard_webhooks_prefix()
    {
        EndpointSecret.Generate().ShouldStartWith("whsec_");
    }

    [Fact]
    public void Generated_secret_carries_32_random_bytes_in_standard_base64()
    {
        var secret = EndpointSecret.Generate();

        var bytes = Convert.FromBase64String(secret[EndpointSecret.Prefix.Length..]);

        bytes.Length.ShouldBe(EndpointSecret.RandomByteLength);
    }

    [Fact]
    public void Secret_size_is_within_the_range_allowed_by_the_specification()
    {
        EndpointSecret.RandomByteLength.ShouldBeInRange(24, 64);
    }

    [Fact]
    public void Each_generated_secret_is_different()
    {
        var secrets = Enumerable.Range(0, 100).Select(_ => EndpointSecret.Generate()).ToHashSet();

        secrets.Count.ShouldBe(100);
    }
}
