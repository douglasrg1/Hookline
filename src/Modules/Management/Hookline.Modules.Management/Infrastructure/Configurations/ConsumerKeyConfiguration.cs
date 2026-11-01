using Hookline.Modules.Management.Domain.ConsumerKeys;
using Hookline.Modules.Management.Domain.Consumers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hookline.Modules.Management.Infrastructure.Configurations;

internal sealed class ConsumerKeyConfiguration : IEntityTypeConfiguration<ConsumerKey>
{
    public void Configure(EntityTypeBuilder<ConsumerKey> builder)
    {
        builder.Property(k => k.Id).ValueGeneratedNever();
        builder.Property(k => k.DisplayPrefix).HasMaxLength(ConsumerKey.DisplayPrefixLength);
        builder.Ignore(k => k.IsRevoked);

        // Authentication finds the key by its hash.
        builder.HasIndex(k => k.Hash).IsUnique();

        // Deleting a consumer deletes its keys.
        builder.HasOne<Consumer>()
            .WithMany()
            .HasForeignKey(k => k.ConsumerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
