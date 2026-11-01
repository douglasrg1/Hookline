using Hookline.Modules.Management.Domain.Consumers;
using Hookline.SharedKernel;

namespace Hookline.Modules.Management.Application.Authentication;

// Who is calling the API: the organization, with access to every consumer, or a single consumer.
internal sealed record Caller
{
    public static readonly Caller Organization = new(consumerId: null);

    private Caller(Guid? consumerId)
    {
        ConsumerId = consumerId;
    }

    // Set only when the caller is a consumer.
    public Guid? ConsumerId { get; }

    public static Caller Consumer(Guid consumerId) => new(consumerId);

    // The organization reaches every consumer, and a consumer reaches only itself. Any other consumer
    // gets the same error as one that does not exist, so it cannot tell the two cases apart.
    public Result CheckAccess(Guid consumerId) =>
        ConsumerId is null || ConsumerId == consumerId ? Result.Success() : ConsumerErrors.NotFound;
}
