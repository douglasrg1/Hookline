using Hookline.Modules.Delivery.Infrastructure;
using Hookline.Modules.Delivery.Presentation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hookline.Modules.Delivery;

public static class DeliveryModule
{
    public static IServiceCollection AddDeliveryModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<DeliveryDbContext>(options => options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", DeliveryDbContext.Schema)));

        return services;
    }

    public static IEndpointRouteBuilder MapDeliveryEndpoints(this IEndpointRouteBuilder endpoints)
        => DeliveryEndpoints.Map(endpoints);
}
