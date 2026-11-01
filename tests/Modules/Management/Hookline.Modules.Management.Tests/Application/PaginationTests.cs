using Hookline.Modules.Management.Application.Pagination;

using Shouldly;

namespace Hookline.Modules.Management.Tests.Application;

public class PaginationTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Limit_defaults_to_fifty()
    {
        PageRequest.Create(after: null, limit: null).Value.Limit.ShouldBe(PageRequest.DefaultLimit);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(PageRequest.MaxLimit)]
    public void Limit_accepts_the_allowed_range(int limit)
    {
        PageRequest.Create(after: null, limit).Value.Limit.ShouldBe(limit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(PageRequest.MaxLimit + 1)]
    public void Limit_rejects_values_out_of_range(int limit)
    {
        PageRequest.Create(after: null, limit).Error.ShouldBe(PageErrors.LimitInvalid);
    }

    [Fact]
    public void Extra_item_means_there_is_a_next_page_after_the_last_item()
    {
        Guid[] fetched = [NewId(), NewId(), NewId()];

        var page = Page.From(fetched, PageRequest.Create(after: null, limit: 2).Value, id => id, id => id);

        page.Items.ShouldBe([fetched[0], fetched[1]]);
        page.Next.ShouldBe(fetched[1]);
    }

    [Fact]
    public void No_extra_item_means_the_last_page()
    {
        Guid[] fetched = [NewId(), NewId()];

        var page = Page.From(fetched, PageRequest.Create(after: null, limit: 2).Value, id => id, id => id);

        page.Items.ShouldBe(fetched);
        page.Next.ShouldBeNull();
    }

    private static Guid NewId() => Guid.CreateVersion7(Now);
}
