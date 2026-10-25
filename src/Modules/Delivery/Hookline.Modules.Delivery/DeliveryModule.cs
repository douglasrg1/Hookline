using Hookline.Modules.Delivery.Infrastructure;
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
}
