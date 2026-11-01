using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Hookline.Api.Tests;

// Replaces the real health check service so the /health endpoint throws the given exception
// from inside the real request pipeline.
internal sealed class ThrowingHealthCheckService(Exception exception) : HealthCheckService
{
    public override Task<HealthReport> CheckHealthAsync(
        Func<HealthCheckRegistration, bool>? predicate,
        CancellationToken cancellationToken = default)
        => throw exception;
}
