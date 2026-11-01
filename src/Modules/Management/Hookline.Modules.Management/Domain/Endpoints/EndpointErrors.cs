using Hookline.SharedKernel;

namespace Hookline.Modules.Management.Domain.Endpoints;

internal static class EndpointErrors
{
    public static readonly Error UrlRequired = Error.Validation(
        "endpoint.url_required",
        "The endpoint URL is required.");

    public static readonly Error UrlTooLong = Error.Validation(
        "endpoint.url_too_long",
        $"The endpoint URL must have at most {Endpoint.UrlMaxLength} characters.");

    public static readonly Error UrlInvalid = Error.Validation(
        "endpoint.url_invalid",
        "The endpoint URL must be an absolute http or https URL.");

    public static readonly Error HttpsRequired = Error.Validation(
        "endpoint.https_required",
        "The endpoint URL must use https. Plain http is accepted only for localhost and 127.0.0.1.");

    public static readonly Error TooManyEventTypes = Error.Validation(
        "endpoint.too_many_event_types",
        $"An endpoint can filter at most {Endpoint.MaxEventTypes} event types.");

    public static Error EventTypeInvalid(string? eventType) => Error.Validation(
        "endpoint.event_type_invalid",
        $"The event type '{eventType}' is invalid. Use lowercase words separated by dots, such as 'invoice.paid', "
        + $"with at most {Endpoint.EventTypeMaxLength} characters.");

    public static Error EventTypeDuplicated(string eventType) => Error.Validation(
        "endpoint.event_type_duplicated",
        $"The event type '{eventType}' appears more than once.");

    public static readonly Error NotFound = Error.NotFound(
        "endpoint.not_found",
        "The endpoint was not found.");

    public static readonly Error UrlTaken = Error.Conflict(
        "endpoint.url_taken",
        "The consumer already has an endpoint with this URL.");
}
