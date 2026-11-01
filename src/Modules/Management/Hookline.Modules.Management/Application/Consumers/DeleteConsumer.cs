using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Domain.Consumers;
using Hookline.SharedKernel;

namespace Hookline.Modules.Management.Application.Consumers;

// Deleting a consumer also deletes its keys and endpoints, so its keys stop working at once.
internal sealed class DeleteConsumer(IConsumerRepository consumers)
{
    public async Task<Result> HandleAsync(Guid consumerId, CancellationToken cancellationToken) =>
        await consumers.DeleteAsync(consumerId, cancellationToken) ? Result.Success() : ConsumerErrors.NotFound;
}
