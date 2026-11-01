using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Domain.Consumers;
using Microsoft.EntityFrameworkCore;

namespace Hookline.Modules.Management.Infrastructure.Repositories;

internal sealed class ConsumerRepository(ManagementDbContext context) : IConsumerRepository
{
    public void Add(Consumer consumer) => context.Consumers.Add(consumer);

    public Task<Consumer?> FindAsync(Guid consumerId, CancellationToken cancellationToken) =>
        context.Consumers.SingleOrDefaultAsync(c => c.Id == consumerId, cancellationToken);

    public Task<bool> ExternalIdExistsAsync(string externalId, CancellationToken cancellationToken) =>
        context.Consumers.AnyAsync(c => c.ExternalId == externalId, cancellationToken);

    public async Task<IReadOnlyList<Consumer>> ListAsync(CancellationToken cancellationToken) =>
        await context.Consumers
            .AsNoTracking()
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

    // A single DELETE statement. The database cascades it to the keys and endpoints.
    public async Task<bool> DeleteAsync(Guid consumerId, CancellationToken cancellationToken) =>
        await context.Consumers.Where(c => c.Id == consumerId).ExecuteDeleteAsync(cancellationToken) > 0;
}
