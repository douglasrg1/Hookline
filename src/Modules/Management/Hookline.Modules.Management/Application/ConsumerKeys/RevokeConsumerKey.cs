using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Domain.ConsumerKeys;
using Hookline.SharedKernel;

namespace Hookline.Modules.Management.Application.ConsumerKeys;

// The key is kept, marked as revoked, so the list still shows when it stopped working.
internal sealed class RevokeConsumerKey(IConsumerKeyRepository keys, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task<Result> HandleAsync(Guid consumerId, Guid keyId, CancellationToken cancellationToken)
    {
        var key = await keys.FindAsync(consumerId, keyId, cancellationToken);

        if (key is null)
        {
            return ConsumerKeyErrors.NotFound;
        }

        key.Revoke(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
