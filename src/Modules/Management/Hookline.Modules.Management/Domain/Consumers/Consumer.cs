using System.Text.RegularExpressions;

using Hookline.SharedKernel;

namespace Hookline.Modules.Management.Domain.Consumers;

// A consumer is one of the organization's customers that receives webhooks.
// It groups the customer's endpoints and is the unit of isolation:
// a consumer never sees another consumer's data.
internal sealed partial class Consumer
{
    public const int NameMaxLength = 100;
    public const int ExternalIdMaxLength = 64;

    private Consumer()
    {
        Name = string.Empty;
    }

    private Consumer(Guid id, string name, string? externalId, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        ExternalId = externalId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    // Optional identifier of this consumer in the organization's own system, such as its customer id.
    public string? ExternalId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<Consumer> Create(string name, string? externalId, DateTimeOffset now)
    {
        var trimmedName = name?.Trim();

        if (string.IsNullOrEmpty(trimmedName))
        {
            return ConsumerErrors.NameRequired;
        }

        if (trimmedName.Length > NameMaxLength)
        {
            return ConsumerErrors.NameTooLong;
        }

        if (externalId is not null && !ExternalIdPattern().IsMatch(externalId))
        {
            return ConsumerErrors.ExternalIdInvalid;
        }

        return new Consumer(Guid.CreateVersion7(now), trimmedName, externalId, now);
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{1,64}$")]
    private static partial Regex ExternalIdPattern();
}
