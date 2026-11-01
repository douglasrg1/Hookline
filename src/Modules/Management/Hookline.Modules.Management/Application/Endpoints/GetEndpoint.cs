using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Domain.Endpoints;
using Hookline.SharedKernel;

namespace Hookline.Modules.Management.Application.Endpoints;

internal sealed class GetEndpoint(IEndpointRepository endpoints)
{
    public async Task<Result<EndpointDto>> HandleAsync(Guid consumerId, Guid endpointId, CancellationToken cancellationToken)
    {
        var endpoint = await endpoints.FindAsync(consumerId, endpointId, cancellationToken);

        return endpoint is null ? EndpointErrors.NotFound : EndpointDto.From(endpoint);
    }
}
