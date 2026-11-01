using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Domain.Consumers;
using Hookline.Modules.Management.Domain.Endpoints;
using Hookline.SharedKernel;

namespace Hookline.Modules.Management.Application.Endpoints;

// Without event types, the endpoint receives every event.
internal sealed record CreateEndpointCommand(string Url, IReadOnlyList<string>? EventTypes);

internal sealed class CreateEndpoint(
    IConsumerRepository consumers,
    IEndpointRepository endpoints,
    ISecretProtector secretProtector,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<EndpointDto>> HandleAsync(
        Guid consumerId,
        CreateEndpointCommand command,
        CancellationToken cancellationToken)
    {
        if (!await consumers.ExistsAsync(consumerId, cancellationToken))
        {
            return ConsumerErrors.NotFound;
        }

        // Generated here and stored encrypted. The plain text secret is read through its own route.
        var encryptedSecret = secretProtector.Protect(EndpointSecret.Generate());

        var created = Endpoint.Create(
            consumerId,
            command.Url,
            command.EventTypes ?? [],
            encryptedSecret,
            timeProvider.GetUtcNow());

        if (created.IsFailure)
        {
            return created.Error;
        }

        var endpoint = created.Value;

        // The unique index still guards against two requests racing. Checking first gives a clear 409.
        if (await endpoints.UrlExistsAsync(consumerId, endpoint.Url, excludingEndpointId: null, cancellationToken))
        {
            return EndpointErrors.UrlTaken;
        }

        endpoints.Add(endpoint);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return EndpointDto.From(endpoint);
    }
}
