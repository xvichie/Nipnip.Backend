using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class StoreDiscountCodeConfiguration : IEntityTypeConfiguration<StoreDiscountCode>
{
    public void Configure(EntityTypeBuilder<StoreDiscountCode> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.Code).IsRequired();
        builder.HasIndex(c => new { c.StoreId, c.Code }).IsUnique();
    }
}
