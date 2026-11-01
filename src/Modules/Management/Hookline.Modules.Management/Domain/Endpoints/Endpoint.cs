using System.Text.RegularExpressions;

using Hookline.SharedKernel;

namespace Hookline.Modules.Management.Domain.Endpoints;

// A destination that receives a consumer's webhooks.
internal sealed partial class Endpoint
{
    public const int UrlMaxLength = 2048;
    public const int MaxEventTypes = 50;
    public const int EventTypeMaxLength = 100;

    private Endpoint()
    {
        Url = string.Empty;
        EventTypes = [];
        EncryptedSecret = [];
    }

    private Endpoint(
        Guid id,
        Guid consumerId,
        string url,
        IReadOnlyList<string> eventTypes,
        byte[] encryptedSecret,
        DateTimeOffset now)
    {
        Id = id;
        ConsumerId = consumerId;
        Url = url;
        EventTypes = eventTypes;
        EncryptedSecret = encryptedSecret;
        IsEnabled = true;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    public Guid ConsumerId { get; private set; }

    public string Url { get; private set; }

    // An empty list means the endpoint receives every event type.
    public IReadOnlyList<string> EventTypes { get; private set; }

    // The signing secret, encrypted outside the domain. The domain never sees it in plain text.
    public byte[] EncryptedSecret { get; private set; }

    public bool IsEnabled { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<Endpoint> Create(
        Guid consumerId,
        string url,
        IReadOnlyCollection<string> eventTypes,
        byte[] encryptedSecret,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(eventTypes);
        ArgumentNullException.ThrowIfNull(encryptedSecret);

        if (encryptedSecret.Length == 0)
        {
            throw new ArgumentException("The encrypted secret must not be empty.", nameof(encryptedSecret));
        }

        if (ValidateUrl(url) is { } urlError)
        {
            return urlError;
        }

        if (ValidateEventTypes(eventTypes) is { } eventTypesError)
        {
            return eventTypesError;
        }

        return new Endpoint(Guid.CreateVersion7(now), consumerId, url, [.. eventTypes], encryptedSecret, now);
    }

    public Result Update(string url, IReadOnlyCollection<string> eventTypes, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(eventTypes);

        if (ValidateUrl(url) is { } urlError)
        {
            return urlError;
        }

        if (ValidateEventTypes(eventTypes) is { } eventTypesError)
        {
            return eventTypesError;
        }

        Url = url;
        EventTypes = [.. eventTypes];
        UpdatedAt = now;

        return Result.Success();
    }

    public void Enable(DateTimeOffset now)
    {
        if (IsEnabled)
        {
            return;
        }

        IsEnabled = true;
        UpdatedAt = now;
    }

    public void Disable(DateTimeOffset now)
    {
        if (!IsEnabled)
        {
            return;
        }

        IsEnabled = false;
        UpdatedAt = now;
    }

    public bool Accepts(string eventType) =>
        EventTypes.Count == 0 || EventTypes.Contains(eventType, StringComparer.Ordinal);

    private static Error? ValidateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return EndpointErrors.UrlRequired;
        }

        if (url.Length > UrlMaxLength)
        {
            return EndpointErrors.UrlTooLong;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return EndpointErrors.UrlInvalid;
        }

        if (uri.Scheme == Uri.UriSchemeHttp && !IsLocalHost(uri))
        {
            return EndpointErrors.HttpsRequired;
        }

        return null;
    }

    // Plain http is accepted only for local development.
    private static bool IsLocalHost(Uri uri) => uri.Host is "localhost" or "127.0.0.1";

    private static Error? ValidateEventTypes(IReadOnlyCollection<string> eventTypes)
    {
        if (eventTypes.Count > MaxEventTypes)
        {
            return EndpointErrors.TooManyEventTypes;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var eventType in eventTypes)
        {
            if (eventType is null || eventType.Length > EventTypeMaxLength || !EventTypePattern().IsMatch(eventType))
            {
                return EndpointErrors.EventTypeInvalid(eventType);
            }

            if (!seen.Add(eventType))
            {
                return EndpointErrors.EventTypeDuplicated(eventType);
            }
        }

        return null;
    }

    // Lowercase words separated by dots, with at least two parts, such as "invoice.paid".
    [GeneratedRegex(@"^[a-z0-9_]+(\.[a-z0-9_]+)+$")]
    private static partial Regex EventTypePattern();
}
