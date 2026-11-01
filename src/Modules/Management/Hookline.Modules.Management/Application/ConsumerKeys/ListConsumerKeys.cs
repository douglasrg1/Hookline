using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Application.Pagination;
using Hookline.Modules.Management.Domain.Consumers;
using Hookline.SharedKernel;

namespace Hookline.Modules.Management.Application.ConsumerKeys;

internal sealed class ListConsumerKeys(IConsumerRepository consumers, IConsumerKeyRepository keys)
{
    public async Task<Result<Page<ConsumerKeyDto>>> HandleAsync(
        Guid consumerId,
        Guid? after,
        int? limit,
        CancellationToken cancellationToken)
    {
        var request = PageRequest.Create(after, limit);

        if (request.IsFailure)
        {
            return request.Error;
        }

        // An empty page would hide that the consumer does not exist.
        if (!await consumers.ExistsAsync(consumerId, cancellationToken))
        {
            return ConsumerErrors.NotFound;
        }

        var fetched = await keys.ListAsync(consumerId, request.Value.After, request.Value.Limit + 1, cancellationToken);

        return Page.From(fetched, request.Value, k => k.Id, ConsumerKeyDto.From);
    }
}
