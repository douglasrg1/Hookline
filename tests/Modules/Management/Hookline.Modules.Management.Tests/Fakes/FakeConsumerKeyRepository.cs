using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Domain.ConsumerKeys;

namespace Hookline.Modules.Management.Tests.Fakes;

// Keeps the keys in memory and counts the lookups, so tests can tell when the database would be hit.
internal sealed class FakeConsumerKeyRepository(params ConsumerKey[] keys) : IConsumerKeyRepository
{
    public int Lookups { get; private set; }

    public Task<ConsumerKey?> FindByHashAsync(byte[] hash, CancellationToken cancellationToken)
    {
        Lookups++;

        return Task.FromResult(keys.SingleOrDefault(k => k.Hash.AsSpan().SequenceEqual(hash)));
    }
}
