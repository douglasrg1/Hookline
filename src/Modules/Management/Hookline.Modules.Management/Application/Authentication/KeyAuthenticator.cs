using System.Security.Cryptography;
using System.Text;

using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Domain.ConsumerKeys;

using Microsoft.Extensions.Options;

namespace Hookline.Modules.Management.Application.Authentication;

// Finds out who is calling from the key sent with the request.
internal sealed class KeyAuthenticator(IOptions<AuthOptions> options, IConsumerKeyRepository consumerKeys)
{
    // Both sides are hashed before comparing: hashes always have the same length, so the comparison
    // takes the same time whatever key is sent and reveals nothing about the organization key.
    private readonly byte[] _organizationKeyHash = Hash(options.Value.OrganizationKey);

    // Returns null when the key is unknown or revoked.
    public async Task<Caller?> AuthenticateAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (CryptographicOperations.FixedTimeEquals(Hash(key), _organizationKeyHash))
        {
            return Caller.Organization;
        }

        // Only consumer keys are looked up in the database.
        if (!key.StartsWith(ConsumerKey.KeyPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var consumerKey = await consumerKeys.FindByHashAsync(ConsumerKey.ComputeHash(key), cancellationToken);

        return consumerKey is { IsRevoked: false } ? Caller.Consumer(consumerKey.ConsumerId) : null;
    }

    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
}
