using Hookline.Modules.Management.Application.Consumers;
using Hookline.Modules.Management.Presentation.Authentication;
using Hookline.SharedKernel.AspNetCore;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Hookline.Modules.Management.Presentation.Consumers;

internal static class ConsumerRoutes
{
    // consumers is /consumers. consumer is /consumers/{consumerId}, behind the consumer access filter.
    public static void Map(RouteGroupBuilder consumers, RouteGroupBuilder consumer)
    {
        consumers.MapPost("", CreateAsync).RequireAuthorization(AuthorizationPolicies.Organization);
        consumers.MapGet("", ListAsync).RequireAuthorization(AuthorizationPolicies.Organization);

        consumer.MapGet("", GetAsync);
        consumer.MapDelete("", DeleteAsync).RequireAuthorization(AuthorizationPolicies.Organization);
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

    private static async Task<Ok<IReadOnlyList<ConsumerDto>>> ListAsync(
        ListConsumers useCase,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await useCase.HandleAsync(cancellationToken));

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
