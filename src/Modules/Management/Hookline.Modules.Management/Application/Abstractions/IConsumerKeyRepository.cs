using Hookline.Modules.Management.Domain.ConsumerKeys;

namespace Hookline.Modules.Management.Application.Abstractions;

internal interface IConsumerKeyRepository
{
    // Saved by IUnitOfWork.
    void Add(ConsumerKey key);

    // Always by the pair: a key id under another consumer is not found.
    Task<ConsumerKey?> FindAsync(Guid consumerId, Guid keyId, CancellationToken cancellationToken);

    // Read only: used to authenticate a request.
    Task<ConsumerKey?> FindByHashAsync(byte[] hash, CancellationToken cancellationToken);

    // The consumer's keys, revoked ones included, ordered by id. At most count keys created after the given id.
    Task<IReadOnlyList<ConsumerKey>> ListAsync(Guid consumerId, Guid? after, int count, CancellationToken cancellationToken);
}
