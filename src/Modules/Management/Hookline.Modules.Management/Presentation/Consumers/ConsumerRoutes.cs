using Hookline.Modules.Management.Application.Consumers;
using Hookline.Modules.Management.Application.Pagination;
using Hookline.Modules.Management.Presentation.Authentication;
using Hookline.SharedKernel.AspNetCore;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Hookline.Modules.Management.Presentation.Consumers;

internal static class ConsumerRoutes
{
    private const string Tag = "Consumers";

    // consumers is /consumers. consumer is /consumers/{consumerId}, behind the consumer access filter.
    public static void Map(RouteGroupBuilder consumers, RouteGroupBuilder consumer)
    {
        // Groups with no prefix of their own, only to tag these routes: nested groups inherit metadata,
        // so a tag on the parent groups would also land on the key and endpoint routes.
        var collection = consumers.MapGroup("").WithTags(Tag);
        var single = consumer.MapGroup("").WithTags(Tag);

        collection.MapPost("", CreateAsync).RequireAuthorization(AuthorizationPolicies.Organization);
        collection.MapGet("", ListAsync).RequireAuthorization(AuthorizationPolicies.Organization);

        single.MapGet("", GetAsync);
        single.MapDelete("", DeleteAsync).RequireAuthorization(AuthorizationPolicies.Organization);
    }

    private static async Task<Results<Created<ConsumerDto>, ProblemHttpResult>> CreateAsync(
        CreateConsumerCommand command,
        CreateConsumer useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Created($"/consumers/{result.Value.Id}", result.Value)
            : result.Error.ToProblem();
    }

    // GET /consumers?after={id}&limit={n}. Without "after", the first page.
    private static async Task<Results<Ok<Page<ConsumerDto>>, ProblemHttpResult>> ListAsync(
        Guid? after,
        int? limit,
        ListConsumers useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.HandleAsync(after, limit, cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<ConsumerDto>, ProblemHttpResult>> GetAsync(
        Guid consumerId,
        GetConsumer useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.HandleAsync(consumerId, cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteAsync(
        Guid consumerId,
        DeleteConsumer useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.HandleAsync(consumerId, cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }
}
