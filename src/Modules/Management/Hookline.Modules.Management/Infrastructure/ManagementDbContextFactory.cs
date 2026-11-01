using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hookline.Modules.Management.Infrastructure;

// Used only by the dotnet-ef tool. Generating a migration does not open a connection,
// so the connection string is a placeholder and the application settings are not needed.
internal sealed class ManagementDbContextFactory : IDesignTimeDbContextFactory<ManagementDbContext>
{
    public ManagementDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ManagementDbContext>();
        ManagementDbContext.Configure(options, "Host=localhost;Database=hookline");

        return new ManagementDbContext(options.Options);
    }
}
