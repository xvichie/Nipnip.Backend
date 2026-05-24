using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class CreatorConfiguration : IEntityTypeConfiguration<Creator>
{
    public void Configure(EntityTypeBuilder<Creator> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.ClerkUserId).IsRequired();
        builder.HasIndex(c => c.ClerkUserId).IsUnique();

        builder.Property(c => c.Name).IsRequired();

        builder.Property(c => c.Slug).IsRequired();
        builder.HasIndex(c => c.Slug).IsUnique();

        builder.Property(c => c.IsActive).HasDefaultValue(true);

        builder.HasMany(c => c.Clicks)
            .WithOne(cl => cl.Creator)
            .HasForeignKey(cl => cl.CreatorId);

        builder.HasMany(c => c.Conversions)
            .WithOne(cn => cn.Creator)
            .HasForeignKey(cn => cn.CreatorId);

        builder.HasMany(c => c.Payouts)
            .WithOne(p => p.Creator)
            .HasForeignKey(p => p.CreatorId);
    }
}
