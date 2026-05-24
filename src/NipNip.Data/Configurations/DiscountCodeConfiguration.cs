using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class DiscountCodeConfiguration : IEntityTypeConfiguration<DiscountCode>
{
    public void Configure(EntityTypeBuilder<DiscountCode> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Code).IsRequired().HasMaxLength(50);

        // Code is unique per merchant
        builder.HasIndex(d => new { d.MerchantId, d.Code }).IsUnique();

        // Each creator can claim at most one code per merchant
        builder.HasIndex(d => new { d.CreatorId, d.MerchantId }).IsUnique();

        builder.HasOne(d => d.Creator)
            .WithMany()
            .HasForeignKey(d => d.CreatorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Merchant)
            .WithMany()
            .HasForeignKey(d => d.MerchantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
