using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Application.Pagination;
using Hookline.SharedKernel;

namespace Hookline.Modules.Management.Application.Consumers;

internal sealed class ListConsumers(IConsumerRepository consumers)
{
    public async Task<Result<Page<ConsumerDto>>> HandleAsync(Guid? after, int? limit, CancellationToken cancellationToken)
    {
        var request = PageRequest.Create(after, limit);

        if (request.IsFailure)
        {
            return request.Error;
        }

        var fetched = await consumers.ListAsync(request.Value.After, request.Value.Limit + 1, cancellationToken);

        return Page.From(fetched, request.Value, c => c.Id, ConsumerDto.From);
    }
}
