using Hookline.Modules.Management.Application.ConsumerKeys;
using Hookline.Modules.Management.Application.Pagination;
using Hookline.Modules.Management.Presentation.Authentication;
using Hookline.SharedKernel.AspNetCore;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Hookline.Modules.Management.Presentation.ConsumerKeys;

internal static class ConsumerKeyRoutes
{
    // consumer is /consumers/{consumerId}, behind the consumer access filter. Keys are managed by the organization only.
    public static void Map(RouteGroupBuilder consumer)
    {
        var keys = consumer.MapGroup("/keys")
            .WithTags("Consumer keys")
            .RequireAuthorization(AuthorizationPolicies.Organization);

        keys.MapPost("", IssueAsync);
        keys.MapGet("", ListAsync);
        keys.MapDelete("/{keyId:guid}", RevokeAsync);
    }

    // There is no route to read a single key, so the response has no Location header.
    private static async Task<Results<Created<IssuedConsumerKeyDto>, ProblemHttpResult>> IssueAsync(
        Guid consumerId,
        IssueConsumerKey useCase,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var result = await useCase.HandleAsync(consumerId, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        response.DoNotCache();

        return TypedResults.Created((string?)null, result.Value);
    }

    private static async Task<Results<Ok<Page<ConsumerKeyDto>>, ProblemHttpResult>> ListAsync(
        Guid consumerId,
        Guid? after,
        int? limit,
        ListConsumerKeys useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.HandleAsync(consumerId, after, limit, cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    // Revoking twice is not an error: the key keeps the time of the first revocation.
    private static async Task<Results<NoContent, ProblemHttpResult>> RevokeAsync(
        Guid consumerId,
        Guid keyId,
        RevokeConsumerKey useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.HandleAsync(consumerId, keyId, cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }
}
