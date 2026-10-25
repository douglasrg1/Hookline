using Microsoft.EntityFrameworkCore;

namespace Hookline.Modules.Delivery.Infrastructure;

internal sealed class DeliveryDbContext(DbContextOptions<DeliveryDbContext> options) : DbContext(options)
{
    internal const string Schema = "delivery";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.HasDefaultSchema(Schema);
}
