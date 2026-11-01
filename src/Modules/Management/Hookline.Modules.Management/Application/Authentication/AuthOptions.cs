namespace Hookline.Modules.Management.Application.Authentication;

// Bound from the "Auth" configuration section.
internal sealed class AuthOptions
{
    public const string SectionName = "Auth";

    // The organization's key, with access to every consumer. It comes from configuration, never from the database.
    public string OrganizationKey { get; set; } = string.Empty;
}
