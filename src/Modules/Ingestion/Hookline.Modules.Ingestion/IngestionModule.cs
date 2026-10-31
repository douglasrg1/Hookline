using Hookline.Modules.Ingestion.Infrastructure;
using Hookline.Modules.Ingestion.Presentation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hookline.Modules.Ingestion;

public static class IngestionModule
{
    public static IServiceCollection AddIngestionModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<IngestionDbContext>(options => options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", IngestionDbContext.Schema)));

        return services;
    }

    public static IEndpointRouteBuilder MapIngestionEndpoints(this IEndpointRouteBuilder endpoints)
        => IngestionEndpoints.Map(endpoints);
}
