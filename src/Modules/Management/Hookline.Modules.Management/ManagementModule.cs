using Hookline.Modules.Management.Application.Abstractions;
using Hookline.Modules.Management.Application.Authentication;
using Hookline.Modules.Management.Infrastructure;
using Hookline.Modules.Management.Infrastructure.Repositories;
using Hookline.Modules.Management.Infrastructure.Security;
using Hookline.Modules.Management.Presentation;
using Hookline.Modules.Management.Presentation.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

        services.AddScoped<IConsumerKeyRepository, ConsumerKeyRepository>();
        services.AddScoped<KeyAuthenticator>();

        // Consumers and their keys live in this module, so it provides the API's only authentication scheme.
        services.AddAuthentication(BearerKeyAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, BearerKeyAuthenticationHandler>(
                BearerKeyAuthenticationHandler.SchemeName,
                configureOptions: null);

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
