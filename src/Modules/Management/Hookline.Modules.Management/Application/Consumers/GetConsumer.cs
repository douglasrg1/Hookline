using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Domain.Consumers;
using Hookline.SharedKernel;

namespace Hookline.Modules.Management.Application.Consumers;

internal sealed class GetConsumer(IConsumerRepository consumers)
{
    public async Task<Result<ConsumerDto>> HandleAsync(Guid consumerId, CancellationToken cancellationToken)
    {
        var consumer = await consumers.FindAsync(consumerId, cancellationToken);

        return consumer is null ? ConsumerErrors.NotFound : ConsumerDto.From(consumer);
    }
}
