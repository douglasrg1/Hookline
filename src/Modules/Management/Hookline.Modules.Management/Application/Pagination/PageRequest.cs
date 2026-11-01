using Hookline.SharedKernel;

namespace Hookline.Modules.Management.Application.Pagination;

// Which page of a list to return: the items after a given id, oldest first, at most Limit of them.
// Ids are version 7 GUIDs, which follow creation time, so "after an id" means "created later".
internal sealed record PageRequest
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 100;

    private PageRequest(Guid? after, int limit)
    {
        After = after;
        Limit = limit;
    }

    // Null for the first page.
    public Guid? After { get; }

    public int Limit { get; }

    public static Result<PageRequest> Create(Guid? after, int? limit)
    {
        var size = limit ?? DefaultLimit;

        return size is < 1 or > MaxLimit ? PageErrors.LimitInvalid : new PageRequest(after, size);
    }
}
