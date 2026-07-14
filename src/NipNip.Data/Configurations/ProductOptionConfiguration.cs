using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class ProductOptionConfiguration : IEntityTypeConfiguration<ProductOption>
{
    public void Configure(EntityTypeBuilder<ProductOption> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedOnAdd();
        builder.Property(o => o.Name).IsRequired();

        builder.HasMany(o => o.Values)
            .WithOne(v => v.ProductOption)
            .HasForeignKey(v => v.ProductOptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
