using Hookline.Modules.Management.Presentation.Authentication;
using Hookline.Modules.Management.Presentation.Consumers;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hookline.Modules.Management.Presentation;

internal static class ManagementEndpoints
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var consumers = endpoints.MapGroup("/consumers").WithTags("Consumers");

        // Everything under one consumer. Only the organization and that consumer get past the filter,
        // so every route added to this group is isolated without doing anything else.
        var consumer = consumers.MapGroup("/{" + ConsumerAccessFilter.RouteParameter + ":guid}")
            .AddEndpointFilter<ConsumerAccessFilter>();

        ConsumerRoutes.Map(consumers, consumer);

        return endpoints;
    }
}
