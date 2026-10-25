using Microsoft.EntityFrameworkCore;

namespace Hookline.Modules.Management.Infrastructure;

internal sealed class ManagementDbContext(DbContextOptions<ManagementDbContext> options) : DbContext(options)
{
    internal const string Schema = "management";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.HasDefaultSchema(Schema);
}
