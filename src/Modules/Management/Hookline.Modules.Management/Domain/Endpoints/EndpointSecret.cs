using System.Security.Cryptography;

namespace Hookline.Modules.Management.Domain.Endpoints;

// Signing secret of an endpoint, in the Standard Webhooks format: "whsec_" followed by
// random bytes in base64. The specification accepts 24 to 64 bytes; Hookline uses 32.
// The plain text secret is encrypted before it reaches the Endpoint entity.
internal static class EndpointSecret
{
    public const string Prefix = "whsec_";
    public const int RandomByteLength = 32;

    public static string Generate() =>
        Prefix + Convert.ToBase64String(RandomNumberGenerator.GetBytes(RandomByteLength));
}
