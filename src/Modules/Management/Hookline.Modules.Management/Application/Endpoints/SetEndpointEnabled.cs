using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Domain.Endpoints;
using Hookline.SharedKernel;

namespace Hookline.Modules.Management.Application.Endpoints;

// A disabled endpoint receives no deliveries until it is enabled again.
internal sealed class SetEndpointEnabled(IEndpointRepository endpoints, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task<Result<EndpointDto>> HandleAsync(
        Guid consumerId,
        Guid endpointId,
        bool enabled,
        CancellationToken cancellationToken)
    {
        var endpoint = await endpoints.FindAsync(consumerId, endpointId, cancellationToken);

        if (endpoint is null)
        {
            return EndpointErrors.NotFound;
        }

        if (enabled)
        {
            endpoint.Enable(timeProvider.GetUtcNow());
        }
        else
        {
            endpoint.Disable(timeProvider.GetUtcNow());
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return EndpointDto.From(endpoint);
    }
}
