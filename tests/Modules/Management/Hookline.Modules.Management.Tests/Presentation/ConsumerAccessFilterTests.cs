using Hookline.Modules.Management.Application.Authentication;
using Hookline.Modules.Management.Presentation.Authentication;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

using Shouldly;

namespace Hookline.Modules.Management.Tests.Presentation;

public class ConsumerAccessFilterTests
{
    private const string RouteResult = "route result";

    private static readonly DateTimeOffset Now = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid ConsumerId = Guid.CreateVersion7(Now);
    private static readonly Guid OtherConsumerId = Guid.CreateVersion7(Now);

    [Fact]
    public async Task Organization_reaches_the_routes_of_any_consumer()
    {
        var (result, reachedRoute) = await InvokeAsync(Caller.Organization, ConsumerId.ToString());

        reachedRoute.ShouldBeTrue();
        result.ShouldBe(RouteResult);
    }

    [Fact]
    public async Task Consumer_reaches_its_own_routes()
    {
        var (result, reachedRoute) = await InvokeAsync(Caller.Consumer(ConsumerId), ConsumerId.ToString());

        reachedRoute.ShouldBeTrue();
        result.ShouldBe(RouteResult);
    }

    [Fact]
    public async Task Consumer_gets_not_found_on_the_routes_of_another_consumer()
    {
        var (result, reachedRoute) = await InvokeAsync(Caller.Consumer(ConsumerId), OtherConsumerId.ToString());

        reachedRoute.ShouldBeFalse();

        var problem = result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        problem.ProblemDetails.Extensions["code"].ShouldBe("consumer.not_found");
    }

    [Fact]
    public async Task Anonymous_request_is_a_configuration_bug()
    {
        await Should.ThrowAsync<InvalidOperationException>(() => InvokeAsync(caller: null, ConsumerId.ToString()));
    }

    [Fact]
    public async Task Route_without_a_consumer_id_is_a_configuration_bug()
    {
        await Should.ThrowAsync<InvalidOperationException>(() => InvokeAsync(Caller.Organization, routeConsumerId: null));
    }

    // Runs the filter as a route group would, with a route that only records that it was reached.
    private static async Task<(object? Result, bool ReachedRoute)> InvokeAsync(Caller? caller, string? routeConsumerId)
    {
        var httpContext = new DefaultHttpContext();

        if (caller is not null)
        {
            httpContext.User = CallerClaims.ToPrincipal(caller, BearerKeyAuthenticationHandler.SchemeName);
        }

        if (routeConsumerId is not null)
        {
            httpContext.Request.RouteValues[ConsumerAccessFilter.RouteParameter] = routeConsumerId;
        }

        var reachedRoute = false;

        var result = await new ConsumerAccessFilter().InvokeAsync(
            new DefaultEndpointFilterInvocationContext(httpContext),
            _ =>
            {
                reachedRoute = true;
                return ValueTask.FromResult<object?>(RouteResult);
            });

        return (result, reachedRoute);
    }
}
