using Hookline.Modules.Management.Application.Authentication;
using Hookline.Modules.Management.Domain.Consumers;

using Shouldly;

namespace Hookline.Modules.Management.Tests.Application;

public class CallerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid ConsumerId = Guid.CreateVersion7(Now);
    private static readonly Guid OtherConsumerId = Guid.CreateVersion7(Now);

    [Fact]
    public void Organization_can_access_any_consumer()
    {
        Caller.Organization.CheckAccess(ConsumerId).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Consumer_can_access_itself()
    {
        Caller.Consumer(ConsumerId).CheckAccess(ConsumerId).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Consumer_gets_not_found_for_another_consumer()
    {
        var access = Caller.Consumer(ConsumerId).CheckAccess(OtherConsumerId);

        access.Error.ShouldBe(ConsumerErrors.NotFound);
    }
}
