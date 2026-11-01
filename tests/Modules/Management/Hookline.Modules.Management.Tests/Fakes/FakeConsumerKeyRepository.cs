using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Domain.ConsumerKeys;

namespace Hookline.Modules.Management.Tests.Fakes;

// Keeps the keys in memory and counts the lookups by hash, so tests can tell when the database would be hit.
internal sealed class FakeConsumerKeyRepository(params ConsumerKey[] keys) : IConsumerKeyRepository
{
    private readonly List<ConsumerKey> _keys = [.. keys];

    public int Lookups { get; private set; }

    public void Add(ConsumerKey key) => _keys.Add(key);

    public Task<ConsumerKey?> FindAsync(Guid consumerId, Guid keyId, CancellationToken cancellationToken) =>
        Task.FromResult(_keys.SingleOrDefault(k => k.ConsumerId == consumerId && k.Id == keyId));

    public Task<ConsumerKey?> FindByHashAsync(byte[] hash, CancellationToken cancellationToken)
    {
        Lookups++;

        return Task.FromResult(_keys.SingleOrDefault(k => k.Hash.AsSpan().SequenceEqual(hash)));
    }

    public Task<IReadOnlyList<ConsumerKey>> ListAsync(
        Guid consumerId,
        Guid? after,
        int count,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ConsumerKey>>(
            [.. _keys.Where(k => k.ConsumerId == consumerId && (after == null || k.Id > after)).OrderBy(k => k.Id).Take(count)]);
}
