namespace Hookline.Modules.Management.Infrastructure.Security;

// Bound from the "Encryption" configuration section.
internal sealed class EncryptionOptions
{
    public const string SectionName = "Encryption";

    // Master key: 32 random bytes, base64 encoded. It never goes to the database.
    public string Key { get; set; } = string.Empty;
}
