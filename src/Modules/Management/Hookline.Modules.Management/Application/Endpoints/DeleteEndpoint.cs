using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Domain.Endpoints;
using Hookline.SharedKernel;

namespace Hookline.Modules.Management.Application.Endpoints;

internal sealed class DeleteEndpoint(IEndpointRepository endpoints)
{
    public async Task<Result> HandleAsync(Guid consumerId, Guid endpointId, CancellationToken cancellationToken) =>
        await endpoints.DeleteAsync(consumerId, endpointId, cancellationToken) ? Result.Success() : EndpointErrors.NotFound;
}
