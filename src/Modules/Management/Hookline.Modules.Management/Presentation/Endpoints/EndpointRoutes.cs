using Hookline.Modules.Management.Application.Endpoints;
using Hookline.Modules.Management.Application.Pagination;
using Hookline.SharedKernel.AspNetCore;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Hookline.Modules.Management.Presentation.Endpoints;

internal static class EndpointRoutes
{
    // consumer is /consumers/{consumerId}, behind the consumer access filter: the organization
    // and the consumer itself manage the consumer's endpoints.
    public static void Map(RouteGroupBuilder consumer)
    {
        var endpoints = consumer.MapGroup("/endpoints").WithTags("Endpoints");

        endpoints.MapPost("", CreateAsync);
        endpoints.MapGet("", ListAsync);

        var endpoint = endpoints.MapGroup("/{endpointId:guid}");

        endpoint.MapGet("", GetAsync);
        endpoint.MapPut("", UpdateAsync);
        endpoint.MapDelete("", DeleteAsync);
        endpoint.MapPost("/enable", EnableAsync);
        endpoint.MapPost("/disable", DisableAsync);
        endpoint.MapGet("/secret", GetSecretAsync);
    }

    private static async Task<Results<Created<EndpointDto>, ProblemHttpResult>> CreateAsync(
        Guid consumerId,
        CreateEndpointCommand command,
        CreateEndpoint useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.HandleAsync(consumerId, command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Created($"/consumers/{consumerId}/endpoints/{result.Value.Id}", result.Value)
            : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<Page<EndpointDto>>, ProblemHttpResult>> ListAsync(
        Guid consumerId,
        Guid? after,
        int? limit,
        ListEndpoints useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.HandleAsync(consumerId, after, limit, cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<EndpointDto>, ProblemHttpResult>> GetAsync(
        Guid consumerId,
        Guid endpointId,
        GetEndpoint useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.HandleAsync(consumerId, endpointId, cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<EndpointDto>, ProblemHttpResult>> UpdateAsync(
        Guid consumerId,
        Guid endpointId,
        UpdateEndpointCommand command,
        UpdateEndpoint useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.HandleAsync(consumerId, endpointId, command, cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteAsync(
        Guid consumerId,
        Guid endpointId,
        DeleteEndpoint useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.HandleAsync(consumerId, endpointId, cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }

    private static Task<Results<Ok<EndpointDto>, ProblemHttpResult>> EnableAsync(
        Guid consumerId,
        Guid endpointId,
        SetEndpointEnabled useCase,
        CancellationToken cancellationToken) =>
        SetEnabledAsync(consumerId, endpointId, enabled: true, useCase, cancellationToken);

    private static Task<Results<Ok<EndpointDto>, ProblemHttpResult>> DisableAsync(
        Guid consumerId,
        Guid endpointId,
        SetEndpointEnabled useCase,
        CancellationToken cancellationToken) =>
        SetEnabledAsync(consumerId, endpointId, enabled: false, useCase, cancellationToken);

    private static async Task<Results<Ok<EndpointSecretDto>, ProblemHttpResult>> GetSecretAsync(
        Guid consumerId,
        Guid endpointId,
        GetEndpointSecret useCase,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var result = await useCase.HandleAsync(consumerId, endpointId, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        response.DoNotCache();

        return TypedResults.Ok(result.Value);
    }

    private static async Task<Results<Ok<EndpointDto>, ProblemHttpResult>> SetEnabledAsync(
        Guid consumerId,
        Guid endpointId,
        bool enabled,
        SetEndpointEnabled useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.HandleAsync(consumerId, endpointId, enabled, cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
