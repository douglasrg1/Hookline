using Microsoft.EntityFrameworkCore;

namespace Hookline.Modules.Ingestion.Infrastructure;

internal sealed class IngestionDbContext(DbContextOptions<IngestionDbContext> options) : DbContext(options)
{
    internal const string Schema = "ingestion";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.HasDefaultSchema(Schema);
}
