using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Domain.ConsumerKeys;
using Microsoft.EntityFrameworkCore;

namespace Hookline.Modules.Management.Infrastructure.Repositories;

internal sealed class ConsumerKeyRepository(ManagementDbContext context) : IConsumerKeyRepository
{
    public Task<ConsumerKey?> FindByHashAsync(byte[] hash, CancellationToken cancellationToken) =>
        context.ConsumerKeys
            .AsNoTracking()
            .SingleOrDefaultAsync(k => k.Hash == hash, cancellationToken);
}
