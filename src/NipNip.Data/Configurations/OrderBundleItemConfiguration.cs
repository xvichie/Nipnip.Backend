using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class OrderBundleItemConfiguration : IEntityTypeConfiguration<OrderBundleItem>
{
    public void Configure(EntityTypeBuilder<OrderBundleItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedOnAdd();

        builder.Property(i => i.PriceAtPurchase).HasColumnType("decimal(18,2)");

        builder.HasOne(i => i.Bundle)
            .WithMany()
            .HasForeignKey(i => i.BundleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
