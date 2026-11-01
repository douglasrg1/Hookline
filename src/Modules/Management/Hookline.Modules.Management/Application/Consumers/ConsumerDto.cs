using Hookline.Modules.Management.Domain.Consumers;

namespace Hookline.Modules.Management.Application.Consumers;

// What the API returns for a consumer.
internal sealed record ConsumerDto(Guid Id, string Name, string? ExternalId, DateTimeOffset CreatedAt)
{
    public static ConsumerDto From(Consumer consumer) =>
        new(consumer.Id, consumer.Name, consumer.ExternalId, consumer.CreatedAt);
}
