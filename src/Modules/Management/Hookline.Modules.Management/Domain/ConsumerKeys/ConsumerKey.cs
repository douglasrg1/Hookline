using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Hookline.Modules.Management.Domain.ConsumerKeys;

// An API key restricted to one consumer. The key is generated here, shown once to the caller
// and never stored: only its SHA-256 hash and a short prefix for listings are kept.
internal sealed class ConsumerKey
{
    public const string KeyPrefix = "hl_";
    public const int DisplayPrefixLength = 12;

    private const int RandomByteLength = 32;

    private ConsumerKey()
    {
        Hash = [];
        DisplayPrefix = string.Empty;
    }

    private ConsumerKey(Guid id, Guid consumerId, byte[] hash, string displayPrefix, DateTimeOffset createdAt)
    {
        Id = id;
        ConsumerId = consumerId;
        Hash = hash;
        DisplayPrefix = displayPrefix;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ConsumerId { get; private set; }

    public byte[] Hash { get; private set; }

    // The first characters of the key, so it can be recognized in a listing without revealing it.
    public string DisplayPrefix { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    // Returns the stored key together with the plain text key, which the caller shows once.
    public static (ConsumerKey Key, string PlainTextKey) Issue(Guid consumerId, DateTimeOffset now)
    {
        var plainTextKey = KeyPrefix + Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(RandomByteLength));

        var key = new ConsumerKey(
            Guid.CreateVersion7(now),
            consumerId,
            ComputeHash(plainTextKey),
            plainTextKey[..DisplayPrefixLength],
            now);

        return (key, plainTextKey);
    }

    // Used both when issuing a key and when authenticating a request with it.
    public static byte[] ComputeHash(string plainTextKey)
    {
        ArgumentNullException.ThrowIfNull(plainTextKey);

        return SHA256.HashData(Encoding.UTF8.GetBytes(plainTextKey));
    }

    // Revoking twice keeps the first revocation time.
    public void Revoke(DateTimeOffset now)
    {
        RevokedAt ??= now;
    }
}
