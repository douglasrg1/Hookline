using Hookline.SharedKernel;

namespace Hookline.Modules.Management.Application.Pagination;

internal static class PageErrors
{
    public static readonly Error LimitInvalid = Error.Validation(
        "page.limit_invalid",
        $"The limit must be between 1 and {PageRequest.MaxLimit}.");
}
