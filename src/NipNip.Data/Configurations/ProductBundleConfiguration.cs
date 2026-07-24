using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class ProductBundleConfiguration : IEntityTypeConfiguration<ProductBundle>
{
    public void Configure(EntityTypeBuilder<ProductBundle> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedOnAdd();

        builder.Property(b => b.Name).IsRequired();
        builder.Property(b => b.Slug).IsRequired();
        builder.HasIndex(b => new { b.StoreId, b.Slug }).IsUnique();

        builder.HasMany(b => b.Items)
            .WithOne(i => i.Bundle)
            .HasForeignKey(i => i.BundleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
