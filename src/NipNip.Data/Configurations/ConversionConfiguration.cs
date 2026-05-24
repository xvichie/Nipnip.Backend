using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class ConversionConfiguration : IEntityTypeConfiguration<Conversion>
{
    public void Configure(EntityTypeBuilder<Conversion> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.OrderId).IsRequired();
        builder.HasIndex(c => new { c.MerchantId, c.OrderId }).IsUnique();

        builder.Property(c => c.OrderAmount).HasColumnType("decimal(18,2)");
        builder.Property(c => c.CommissionAmount).HasColumnType("decimal(18,2)");
        builder.Property(c => c.Currency).HasDefaultValue("GEL");

        builder.HasOne(c => c.Click)
            .WithMany()
            .HasForeignKey(c => c.ClickId)
            .IsRequired(false);
    }
}
