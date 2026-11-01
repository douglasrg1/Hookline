namespace Hookline.Modules.Management.Application.Pagination;

// One page of a list. Next is the value to send as "after" to get the following page, or null on the last one.
internal sealed record Page<TItem>(IReadOnlyList<TItem> Items, Guid? Next);

internal static class Page
{
    // Repositories are asked for one item more than the limit: if that extra item comes back,
    // there is a next page, and it starts after the last item of this one.
    public static Page<TItem> From<TSource, TItem>(
        IReadOnlyList<TSource> fetched,
        PageRequest request,
        Func<TSource, Guid> idOf,
        Func<TSource, TItem> map)
    {
        var items = fetched.Take(request.Limit).ToList();
        Guid? next = fetched.Count > request.Limit ? idOf(items[^1]) : null;

        return new Page<TItem>([.. items.Select(map)], next);
    }
}
