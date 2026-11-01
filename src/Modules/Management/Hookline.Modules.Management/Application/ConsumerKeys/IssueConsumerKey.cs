using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Domain.ConsumerKeys;
using Hookline.Modules.Management.Domain.Consumers;
using Hookline.SharedKernel;

namespace Hookline.Modules.Management.Application.ConsumerKeys;

internal sealed class IssueConsumerKey(
    IConsumerRepository consumers,
    IConsumerKeyRepository keys,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<IssuedConsumerKeyDto>> HandleAsync(Guid consumerId, CancellationToken cancellationToken)
    {
        if (!await consumers.ExistsAsync(consumerId, cancellationToken))
        {
            return ConsumerErrors.NotFound;
        }

        var (key, plainTextKey) = ConsumerKey.Issue(consumerId, timeProvider.GetUtcNow());

        keys.Add(key);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new IssuedConsumerKeyDto(key.Id, key.ConsumerId, plainTextKey, key.DisplayPrefix, key.CreatedAt);
    }
}
