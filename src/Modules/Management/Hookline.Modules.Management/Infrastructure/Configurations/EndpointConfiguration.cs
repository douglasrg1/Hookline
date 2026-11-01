using Hookline.Modules.Management.Domain.Consumers;
using Hookline.Modules.Management.Domain.Endpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hookline.Modules.Management.Infrastructure.Configurations;

internal sealed class EndpointConfiguration : IEntityTypeConfiguration<Endpoint>
{
    public void Configure(EntityTypeBuilder<Endpoint> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Url).HasMaxLength(Endpoint.UrlMaxLength);

        // Stored as a PostgreSQL array.
        builder.PrimitiveCollection(e => e.EventTypes)
            .ElementType(type => type.HasMaxLength(Endpoint.EventTypeMaxLength));

        // A consumer cannot register the same URL twice. The index also serves the listing by consumer.
        builder.HasIndex(e => new { e.ConsumerId, e.Url }).IsUnique();

        // Deleting a consumer deletes its endpoints.
        builder.HasOne<Consumer>()
            .WithMany()
            .HasForeignKey(e => e.ConsumerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
