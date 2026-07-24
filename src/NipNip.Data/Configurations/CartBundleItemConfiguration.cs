using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class CartBundleItemConfiguration : IEntityTypeConfiguration<CartBundleItem>
{
    public void Configure(EntityTypeBuilder<CartBundleItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedOnAdd();

        builder.HasIndex(i => new { i.CartId, i.BundleId }).IsUnique();

        builder.HasOne(i => i.Bundle)
            .WithMany()
            .HasForeignKey(i => i.BundleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
