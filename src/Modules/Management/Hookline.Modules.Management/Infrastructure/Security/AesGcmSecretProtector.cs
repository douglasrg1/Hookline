using System.Security.Cryptography;
using System.Text;

using Hookline.Modules.Management.Application.Abstractions;

using Microsoft.Extensions.Options;

namespace Hookline.Modules.Management.Infrastructure.Security;

// Encrypts secrets with AES-256-GCM, which also authenticates them: any change to the stored
// bytes, or the wrong key, makes decryption fail instead of returning garbage.
//
// Layout of the protected data:
// [key version (1 byte)][nonce (12 bytes)][ciphertext (same size as the plain text)][tag (16 bytes)]
internal sealed class AesGcmSecretProtector : ISecretProtector
{
    // Identifies which master key encrypted the data. It is written as the first byte, so a future
    // key rotation can add version 2 and still read the values written with version 1.
    private const byte CurrentKeyVersion = 1;

    // AES-256 needs a 256-bit key, which is 32 bytes.
    private const int KeyLength = 32;

    // 12 bytes is the nonce size recommended for AES-GCM.
    private const int NonceLength = 12;

    // 16 bytes is the full-size authentication tag, the strongest option AES-GCM offers.
    private const int TagLength = 16;

    // Bytes that come before the ciphertext: the version byte plus the nonce.
    private const int HeaderLength = 1 + NonceLength;

    // Same error for every invalid key, pointing to the exact setting to fix.
    // Also used by the startup check in ManagementModule.
    internal static readonly string InvalidKeyMessage =
        $"'{EncryptionOptions.SectionName}:{nameof(EncryptionOptions.Key)}' must be a base64-encoded key of {KeyLength} bytes.";

    // The master key, decoded once from configuration. It lives only in memory, never in the database.
    private readonly byte[] _key;

    public AesGcmSecretProtector(IOptions<EncryptionOptions> options)
    {
        // Fails fast if the options were not provided at all.
        ArgumentNullException.ThrowIfNull(options);

        // The API already checks the key at startup. Checking again here keeps the class safe
        // when it is created directly, as the tests do.
        if (!TryDecodeKey(options.Value.Key, out _key))
        {
            throw new InvalidOperationException(InvalidKeyMessage);
        }
    }

    // The startup check in ManagementModule uses this, so both apply exactly the same rules.
    internal static bool IsValidKey(string? encodedKey) => TryDecodeKey(encodedKey, out _);

    public byte[] Protect(string plainText)
    {
        // A null secret is a programming error, not a user error.
        ArgumentNullException.ThrowIfNull(plainText);

        // AES works on bytes, so the text is converted with a fixed encoding (UTF-8).
        var plainBytes = Encoding.UTF8.GetBytes(plainText);

        // One buffer for everything: header, ciphertext (same size as the plain text) and tag.
        var output = new byte[HeaderLength + plainBytes.Length + TagLength];

        // First byte: which master key version is used, so Unprotect knows which key to pick.
        output[0] = CurrentKeyVersion;

        // The nonce occupies the next 12 bytes. AsSpan points into the buffer without copying.
        var nonce = output.AsSpan(1, NonceLength);

        // A new random nonce for every call. Reusing a nonce with the same key would break AES-GCM,
        // and a random one also makes the same secret encrypt to different bytes each time.
        RandomNumberGenerator.Fill(nonce);

        // The region of the buffer where the encrypted bytes will be written.
        var cipherText = output.AsSpan(HeaderLength, plainBytes.Length);

        // The last 16 bytes, where the authentication tag will be written.
        var tag = output.AsSpan(HeaderLength + plainBytes.Length, TagLength);

        // Creates the AES-GCM engine with the master key. "using" clears it from memory afterwards.
        using var aes = new AesGcm(_key, TagLength);

        // Encrypts the plain text into cipherText and computes the tag. The version byte is passed as
        // associated data: it is not encrypted, but it is covered by the tag, so it cannot be changed unnoticed.
        aes.Encrypt(nonce, plainBytes, cipherText, tag, associatedData: output.AsSpan(0, 1));

        // The buffer now holds version, nonce, ciphertext and tag: everything Unprotect needs except the key.
        return output;
    }

    public string Unprotect(byte[] protectedData)
    {
        // A null input is a programming error.
        ArgumentNullException.ThrowIfNull(protectedData);

        // Anything shorter than version + nonce + tag cannot be valid data.
        if (protectedData.Length < HeaderLength + TagLength)
        {
            throw new CryptographicException("The protected data is too short.");
        }

        // Only version 1 exists today. With a key rotation, this is where the matching key would be picked.
        if (protectedData[0] != CurrentKeyVersion)
        {
            throw new CryptographicException($"The protected data uses an unknown key version: {protectedData[0]}.");
        }

        // Whatever is left between the header and the tag is the ciphertext.
        var cipherTextLength = protectedData.Length - HeaderLength - TagLength;

        // Reads the nonce back from the same position Protect wrote it.
        var nonce = protectedData.AsSpan(1, NonceLength);

        // Reads the ciphertext region.
        var cipherText = protectedData.AsSpan(HeaderLength, cipherTextLength);

        // Reads the tag from the last 16 bytes.
        var tag = protectedData.AsSpan(HeaderLength + cipherTextLength, TagLength);

        // Room for the decrypted bytes, which have the same size as the ciphertext.
        var plainBytes = new byte[cipherTextLength];

        // Same engine and key used to encrypt.
        using var aes = new AesGcm(_key, TagLength);

        // Checks the tag first and only then decrypts. If any byte was changed, the version included,
        // or the key is wrong, this throws AuthenticationTagMismatchException and returns nothing.
        aes.Decrypt(nonce, cipherText, tag, plainBytes, associatedData: protectedData.AsSpan(0, 1));

        // Converts the bytes back to text with the same encoding used in Protect.
        return Encoding.UTF8.GetString(plainBytes);
    }

    private static bool TryDecodeKey(string? encodedKey, out byte[] key)
    {
        // A buffer with exactly the expected size: a longer key will not fit and fails the decode below.
        key = new byte[KeyLength];

        // An empty setting means the key was never configured. Text that is not base64,
        // or that decodes to fewer than 32 bytes, is rejected too.
        return !string.IsNullOrWhiteSpace(encodedKey)
            && Convert.TryFromBase64String(encodedKey, key, out var written)
            && written == KeyLength;
    }
}
