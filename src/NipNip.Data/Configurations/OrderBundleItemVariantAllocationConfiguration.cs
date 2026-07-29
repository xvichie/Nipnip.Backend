using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class OrderBundleItemVariantAllocationConfiguration : IEntityTypeConfiguration<OrderBundleItemVariantAllocation>
{
    public void Configure(EntityTypeBuilder<OrderBundleItemVariantAllocation> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd();

        builder.HasOne(a => a.OrderBundleItem)
            .WithMany(i => i.StockAllocations)
            .HasForeignKey(a => a.OrderBundleItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict, not Cascade — a variant that still has an allocation recorded against it
        // can't be silently dropped out from under an order's stock history.
        builder.HasOne(a => a.Variant)
            .WithMany()
            .HasForeignKey(a => a.VariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
