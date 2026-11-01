using Hookline.SharedKernel;

namespace Hookline.Modules.Management.Domain.ConsumerKeys;

internal static class ConsumerKeyErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "consumer_key.not_found",
        "The consumer key was not found.");
}
