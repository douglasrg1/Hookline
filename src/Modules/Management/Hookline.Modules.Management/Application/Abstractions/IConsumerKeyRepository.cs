using Hookline.Modules.Management.Domain.ConsumerKeys;

namespace Hookline.Modules.Management.Application.Abstractions;

internal interface IConsumerKeyRepository
{
    // Read only: used to authenticate a request.
    Task<ConsumerKey?> FindByHashAsync(byte[] hash, CancellationToken cancellationToken);
}
