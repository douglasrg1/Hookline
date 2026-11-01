using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Application.Authentication;
using Hookline.Modules.Management.Application.ConsumerKeys;
using Hookline.Modules.Management.Application.Consumers;
using Hookline.Modules.Management.Application.Endpoints;
using Hookline.Modules.Management.Infrastructure;
using Hookline.Modules.Management.Infrastructure.Repositories;
using Hookline.Modules.Management.Infrastructure.Security;
using Hookline.Modules.Management.Presentation;
using Hookline.Modules.Management.Presentation.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hookline.Modules.Management;

public static class ManagementModule
{
    public static IServiceCollection AddManagementModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ManagementDbContext>(options => ManagementDbContext.Configure(options, connectionString));

        // Required settings are checked when the API starts, so a missing or invalid one stops it right away.
        services.AddOptions<AuthOptions>()
            .BindConfiguration(AuthOptions.SectionName)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.OrganizationKey),
                $"'{AuthOptions.SectionName}:{nameof(AuthOptions.OrganizationKey)}' is not configured.")
            .ValidateOnStart();

        services.AddOptions<EncryptionOptions>()
            .BindConfiguration(EncryptionOptions.SectionName)
            .Validate(options => AesGcmSecretProtector.IsValidKey(options.Key), AesGcmSecretProtector.InvalidKeyMessage)
            .ValidateOnStart();

        services.AddSingleton<ISecretProtector, AesGcmSecretProtector>();
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ManagementDbContext>());
        services.AddScoped<IConsumerRepository, ConsumerRepository>();
        services.AddScoped<IConsumerKeyRepository, ConsumerKeyRepository>();
        services.AddScoped<IEndpointRepository, EndpointRepository>();

        services.AddScoped<KeyAuthenticator>();
        services.AddScoped<CreateConsumer>();
        services.AddScoped<ListConsumers>();
        services.AddScoped<GetConsumer>();
        services.AddScoped<DeleteConsumer>();
        services.AddScoped<IssueConsumerKey>();
        services.AddScoped<ListConsumerKeys>();
        services.AddScoped<RevokeConsumerKey>();
        services.AddScoped<CreateEndpoint>();
        services.AddScoped<ListEndpoints>();
        services.AddScoped<GetEndpoint>();
        services.AddScoped<UpdateEndpoint>();
        services.AddScoped<DeleteEndpoint>();
        services.AddScoped<SetEndpointEnabled>();
        services.AddScoped<GetEndpointSecret>();

        // Consumers and their keys live in this module, so it provides the API's only authentication scheme.
        services.AddAuthentication(BearerKeyAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, BearerKeyAuthenticationHandler>(
                BearerKeyAuthenticationHandler.SchemeName,
                configureOptions: null);

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.Organization, policy => policy.RequireRole(CallerClaims.OrganizationRole));

        return services;
    }

    // Called by the host when Database:ApplyMigrationsOnStartup is on.
    public static async Task ApplyManagementMigrationsAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ManagementDbContext>();

        await context.Database.MigrateAsync(cancellationToken);
    }

    public static IEndpointRouteBuilder MapManagementEndpoints(this IEndpointRouteBuilder endpoints)
        => ManagementEndpoints.Map(endpoints);
}
