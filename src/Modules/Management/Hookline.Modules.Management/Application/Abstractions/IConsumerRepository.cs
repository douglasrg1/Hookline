using Hookline.Modules.Management.Domain.Consumers;

namespace Hookline.Modules.Management.Application.Abstractions;

internal interface IConsumerRepository
{
    // Saved by IUnitOfWork.
    void Add(Consumer consumer);

    Task<Consumer?> FindAsync(Guid consumerId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid consumerId, CancellationToken cancellationToken);

    Task<bool> ExternalIdExistsAsync(string externalId, CancellationToken cancellationToken);

    // Ordered by id, which follows creation time. Returns at most count consumers created after the given id.
    Task<IReadOnlyList<Consumer>> ListAsync(Guid? after, int count, CancellationToken cancellationToken);

    // Deletes right away, together with the consumer's keys and endpoints. False when it does not exist.
    Task<bool> DeleteAsync(Guid consumerId, CancellationToken cancellationToken);
}
