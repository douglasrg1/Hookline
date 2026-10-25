using Hookline.Modules.Management.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hookline.Modules.Management;

public static class ManagementModule
{
    public static IServiceCollection AddManagementModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ManagementDbContext>(options => options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", ManagementDbContext.Schema)));

        return services;
    }
}
