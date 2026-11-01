using Hookline.Modules.Management.Domain.Endpoints;

namespace Hookline.Modules.Management.Application.Abstractions;

internal interface IEndpointRepository
{
    // Saved by IUnitOfWork.
    void Add(Endpoint endpoint);

    // Always by the pair: an endpoint id under another consumer is not found.
    Task<Endpoint?> FindAsync(Guid consumerId, Guid endpointId, CancellationToken cancellationToken);

    // Whether another endpoint of the consumer already uses the URL. The endpoint being updated is left out.
    Task<bool> UrlExistsAsync(Guid consumerId, string url, Guid? excludingEndpointId, CancellationToken cancellationToken);

    // The consumer's endpoints, ordered by id. At most count endpoints created after the given id.
    Task<IReadOnlyList<Endpoint>> ListAsync(Guid consumerId, Guid? after, int count, CancellationToken cancellationToken);

    // Deletes right away. False when the consumer has no endpoint with that id.
    Task<bool> DeleteAsync(Guid consumerId, Guid endpointId, CancellationToken cancellationToken);
}
