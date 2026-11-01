using Hookline.Modules.Management.Domain.ConsumerKeys;

namespace Hookline.Modules.Management.Application.ConsumerKeys;

// What the API returns for a key. The key itself is never returned again after it is issued:
// only its first characters, so it can be recognized in a list.
internal sealed record ConsumerKeyDto(
    Guid Id,
    Guid ConsumerId,
    string DisplayPrefix,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RevokedAt)
{
    public static ConsumerKeyDto From(ConsumerKey key) =>
        new(key.Id, key.ConsumerId, key.DisplayPrefix, key.CreatedAt, key.RevokedAt);
}

// Returned once, when the key is issued: the only time the plain text key leaves the API.
internal sealed record IssuedConsumerKeyDto(
    Guid Id,
    Guid ConsumerId,
    string Key,
    string DisplayPrefix,
    DateTimeOffset CreatedAt);
