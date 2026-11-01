using Hookline.Modules.Management.Domain.Endpoints;

namespace Hookline.Modules.Management.Application.Endpoints;

// What the API returns for an endpoint. The signing secret has its own route.
internal sealed record EndpointDto(
    Guid Id,
    Guid ConsumerId,
    string Url,
    IReadOnlyList<string> EventTypes,
    bool IsEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static EndpointDto From(Endpoint endpoint) =>
        new(
            endpoint.Id,
            endpoint.ConsumerId,
            endpoint.Url,
            endpoint.EventTypes,
            endpoint.IsEnabled,
            endpoint.CreatedAt,
            endpoint.UpdatedAt);
}
