using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Domain.Endpoints;
using Hookline.SharedKernel;

namespace Hookline.Modules.Management.Application.Endpoints;

internal sealed record EndpointSecretDto(string Secret);

// The consumer uses the secret to check the signature of every webhook its endpoint receives.
internal sealed class GetEndpointSecret(IEndpointRepository endpoints, ISecretProtector secretProtector)
{
    public async Task<Result<EndpointSecretDto>> HandleAsync(
        Guid consumerId,
        Guid endpointId,
        CancellationToken cancellationToken)
    {
        var endpoint = await endpoints.FindAsync(consumerId, endpointId, cancellationToken);

        return endpoint is null
            ? EndpointErrors.NotFound
            : new EndpointSecretDto(secretProtector.Unprotect(endpoint.EncryptedSecret));
    }
}
