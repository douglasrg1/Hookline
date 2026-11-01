using Hookline.Modules.Management.Application.Abstractions;

namespace Hookline.Modules.Management.Application.Consumers;

internal sealed class ListConsumers(IConsumerRepository consumers)
{
    public async Task<IReadOnlyList<ConsumerDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var all = await consumers.ListAsync(cancellationToken);

        return [.. all.Select(ConsumerDto.From)];
    }
}
