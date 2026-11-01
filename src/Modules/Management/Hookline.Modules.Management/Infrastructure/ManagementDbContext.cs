using Hookline.Modules.Management.Domain.ConsumerKeys;
using Hookline.Modules.Management.Domain.Consumers;
using Hookline.Modules.Management.Domain.Endpoints;
using Microsoft.EntityFrameworkCore;

namespace Hookline.Modules.Management.Infrastructure;

internal sealed class ManagementDbContext(DbContextOptions<ManagementDbContext> options) : DbContext(options)
{
    internal const string Schema = "management";

    public DbSet<Consumer> Consumers => Set<Consumer>();

    public DbSet<ConsumerKey> ConsumerKeys => Set<ConsumerKey>();

    public DbSet<Endpoint> Endpoints => Set<Endpoint>();

    // Used by the application and by the design-time factory, so migrations are generated
    // with the same options the application runs with.
    internal static void Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .UseSnakeCaseNamingConvention();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ManagementDbContext).Assembly);
    }
}
