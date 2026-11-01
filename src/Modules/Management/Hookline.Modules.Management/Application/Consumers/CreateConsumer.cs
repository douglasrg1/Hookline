using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Domain.Consumers;
using Hookline.SharedKernel;

namespace Hookline.Modules.Management.Application.Consumers;

internal sealed record CreateConsumerCommand(string Name, string? ExternalId);

internal sealed class CreateConsumer(IConsumerRepository consumers, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task<Result<ConsumerDto>> HandleAsync(CreateConsumerCommand command, CancellationToken cancellationToken)
    {
        var created = Consumer.Create(command.Name, command.ExternalId, timeProvider.GetUtcNow());

        if (created.IsFailure)
        {
            return created.Error;
        }

        var consumer = created.Value;

        // The unique index still guards against two requests racing. Checking first gives a clear 409.
        if (consumer.ExternalId is not null && await consumers.ExternalIdExistsAsync(consumer.ExternalId, cancellationToken))
        {
            return ConsumerErrors.ExternalIdTaken;
        }

        consumers.Add(consumer);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ConsumerDto.From(consumer);
    }
}
