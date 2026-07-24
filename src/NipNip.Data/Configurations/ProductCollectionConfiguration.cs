using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class ProductCollectionConfiguration : IEntityTypeConfiguration<ProductCollection>
{
    public void Configure(EntityTypeBuilder<ProductCollection> builder)
    {
        builder.HasKey(pc => pc.Id);
        builder.Property(pc => pc.Id).ValueGeneratedOnAdd();

        builder.HasIndex(pc => new { pc.ProductId, pc.CollectionId }).IsUnique();

        // Unlike Category (Restrict — a category can't be deleted while products reference it),
        // a Collection is just a loose grouping: deleting either side simply drops the membership
        // row, it never blocks the delete.
        builder.HasOne(pc => pc.Product)
            .WithMany(p => p.ProductCollections)
            .HasForeignKey(pc => pc.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pc => pc.Collection)
            .WithMany(c => c.ProductCollections)
            .HasForeignKey(pc => pc.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
