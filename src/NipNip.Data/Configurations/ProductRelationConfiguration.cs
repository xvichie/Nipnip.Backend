using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class ProductRelationConfiguration : IEntityTypeConfiguration<ProductRelation>
{
    public void Configure(EntityTypeBuilder<ProductRelation> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();

        builder.HasIndex(r => new { r.ProductId, r.RelatedProductId }).IsUnique();

        // Deleting the source product clears its own picks. Deleting a product that
        // happens to be someone else's pick is restricted instead — two cascade paths
        // to the same table is the kind of thing that's better avoided outright than
        // debugged later, and silently losing a merchant's curated pick on an unrelated
        // delete elsewhere would be a surprising side effect.
        builder.HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.RelatedProduct)
            .WithMany()
            .HasForeignKey(r => r.RelatedProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
