using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Domain.Endpoints;
using Hookline.SharedKernel;

namespace Hookline.Modules.Management.Application.Endpoints;

// Replaces the URL and the event types. Without event types, the endpoint receives every event.
internal sealed record UpdateEndpointCommand(string Url, IReadOnlyList<string>? EventTypes);

internal sealed class UpdateEndpoint(IEndpointRepository endpoints, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task<Result<EndpointDto>> HandleAsync(
        Guid consumerId,
        Guid endpointId,
        UpdateEndpointCommand command,
        CancellationToken cancellationToken)
    {
        var endpoint = await endpoints.FindAsync(consumerId, endpointId, cancellationToken);

        if (endpoint is null)
        {
            return EndpointErrors.NotFound;
        }

        var updated = endpoint.Update(command.Url, command.EventTypes ?? [], timeProvider.GetUtcNow());

        if (updated.IsFailure)
        {
            return updated.Error;
        }

        // Returning here leaves the change unsaved: it is dropped with the request.
        if (await endpoints.UrlExistsAsync(consumerId, endpoint.Url, excludingEndpointId: endpoint.Id, cancellationToken))
        {
            return EndpointErrors.UrlTaken;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return EndpointDto.From(endpoint);
    }
}
