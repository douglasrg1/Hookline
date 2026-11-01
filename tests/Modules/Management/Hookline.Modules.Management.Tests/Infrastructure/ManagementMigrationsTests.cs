using Hookline.Modules.Management.Infrastructure;
using Microsoft.EntityFrameworkCore;

using Shouldly;

namespace Hookline.Modules.Management.Tests.Infrastructure;

public class ManagementMigrationsTests
{
    // Fails when the model changes and no migration was generated for it. Needs no database.
    [Fact]
    public void Migrations_are_up_to_date_with_the_model()
    {
        using var context = new ManagementDbContextFactory().CreateDbContext([]);

        context.Database.HasPendingModelChanges().ShouldBeFalse();
    }
}
