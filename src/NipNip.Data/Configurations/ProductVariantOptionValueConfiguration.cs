using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class ProductVariantOptionValueConfiguration : IEntityTypeConfiguration<ProductVariantOptionValue>
{
    public void Configure(EntityTypeBuilder<ProductVariantOptionValue> builder)
    {
        builder.HasKey(x => new { x.VariantId, x.OptionValueId });

        builder.HasOne(x => x.Variant)
            .WithMany(v => v.OptionValues)
            .HasForeignKey(x => x.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.OptionValue)
            .WithMany()
            .HasForeignKey(x => x.OptionValueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
