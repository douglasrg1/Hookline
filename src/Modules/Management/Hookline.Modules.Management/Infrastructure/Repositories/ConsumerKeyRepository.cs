using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Domain.ConsumerKeys;
using Microsoft.EntityFrameworkCore;

namespace Hookline.Modules.Management.Infrastructure.Repositories;

internal sealed class ConsumerKeyRepository(ManagementDbContext context) : IConsumerKeyRepository
{
    public void Add(ConsumerKey key) => context.ConsumerKeys.Add(key);

    public Task<ConsumerKey?> FindAsync(Guid consumerId, Guid keyId, CancellationToken cancellationToken) =>
        context.ConsumerKeys.SingleOrDefaultAsync(k => k.ConsumerId == consumerId && k.Id == keyId, cancellationToken);

    public Task<ConsumerKey?> FindByHashAsync(byte[] hash, CancellationToken cancellationToken) =>
        context.ConsumerKeys
            .AsNoTracking()
            .SingleOrDefaultAsync(k => k.Hash == hash, cancellationToken);

    public async Task<IReadOnlyList<ConsumerKey>> ListAsync(
        Guid consumerId,
        Guid? after,
        int count,
        CancellationToken cancellationToken) =>
        await context.ConsumerKeys
            .AsNoTracking()
            .Where(k => k.ConsumerId == consumerId && (after == null || k.Id > after))
            .OrderBy(k => k.Id)
            .Take(count)
            .ToListAsync(cancellationToken);
}
