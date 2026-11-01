using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Domain.Consumers;
using Microsoft.EntityFrameworkCore;

namespace Hookline.Modules.Management.Infrastructure.Repositories;

internal sealed class ConsumerRepository(ManagementDbContext context) : IConsumerRepository
{
    public void Add(Consumer consumer) => context.Consumers.Add(consumer);

    public Task<Consumer?> FindAsync(Guid consumerId, CancellationToken cancellationToken) =>
        context.Consumers.SingleOrDefaultAsync(c => c.Id == consumerId, cancellationToken);

    public Task<bool> ExistsAsync(Guid consumerId, CancellationToken cancellationToken) =>
        context.Consumers.AnyAsync(c => c.Id == consumerId, cancellationToken);

    public Task<bool> ExternalIdExistsAsync(string externalId, CancellationToken cancellationToken) =>
        context.Consumers.AnyAsync(c => c.ExternalId == externalId, cancellationToken);

    // Keyset pagination: the primary key index finds where the page starts, however deep the page is.
    public async Task<IReadOnlyList<Consumer>> ListAsync(Guid? after, int count, CancellationToken cancellationToken) =>
        await context.Consumers
            .AsNoTracking()
            .Where(c => after == null || c.Id > after)
            .OrderBy(c => c.Id)
            .Take(count)
            .ToListAsync(cancellationToken);

    // A single DELETE statement. The database cascades it to the keys and endpoints.
    public async Task<bool> DeleteAsync(Guid consumerId, CancellationToken cancellationToken) =>
        await context.Consumers.Where(c => c.Id == consumerId).ExecuteDeleteAsync(cancellationToken) > 0;
}
