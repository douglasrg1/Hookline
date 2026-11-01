using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Domain.Endpoints;
using Microsoft.EntityFrameworkCore;

namespace Hookline.Modules.Management.Infrastructure.Repositories;

internal sealed class EndpointRepository(ManagementDbContext context) : IEndpointRepository
{
    public void Add(Endpoint endpoint) => context.Endpoints.Add(endpoint);

    public Task<Endpoint?> FindAsync(Guid consumerId, Guid endpointId, CancellationToken cancellationToken) =>
        context.Endpoints.SingleOrDefaultAsync(e => e.ConsumerId == consumerId && e.Id == endpointId, cancellationToken);

    public Task<bool> UrlExistsAsync(
        Guid consumerId,
        string url,
        Guid? excludingEndpointId,
        CancellationToken cancellationToken) =>
        context.Endpoints.AnyAsync(
            e => e.ConsumerId == consumerId && e.Url == url && (excludingEndpointId == null || e.Id != excludingEndpointId),
            cancellationToken);

    public async Task<IReadOnlyList<Endpoint>> ListAsync(
        Guid consumerId,
        Guid? after,
        int count,
        CancellationToken cancellationToken) =>
        await context.Endpoints
            .AsNoTracking()
            .Where(e => e.ConsumerId == consumerId && (after == null || e.Id > after))
            .OrderBy(e => e.Id)
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task<bool> DeleteAsync(Guid consumerId, Guid endpointId, CancellationToken cancellationToken) =>
        await context.Endpoints
            .Where(e => e.ConsumerId == consumerId && e.Id == endpointId)
            .ExecuteDeleteAsync(cancellationToken) > 0;
}
