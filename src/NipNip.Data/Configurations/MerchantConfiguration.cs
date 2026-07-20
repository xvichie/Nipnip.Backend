using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class MerchantConfiguration : IEntityTypeConfiguration<Merchant>
{
    public void Configure(EntityTypeBuilder<Merchant> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd();

        builder.Property(m => m.ClerkUserId).IsRequired();
        builder.HasIndex(m => m.ClerkUserId).IsUnique();

        builder.Property(m => m.Name).IsRequired();

        builder.Property(m => m.Slug).IsRequired();
        builder.HasIndex(m => m.Slug).IsUnique();

        builder.Property(m => m.ApiKey).IsRequired();
        builder.HasIndex(m => m.ApiKey).IsUnique();

        builder.Property(m => m.CommissionPercent).HasColumnType("decimal(18,2)");
        builder.Property(m => m.Balance).HasColumnType("decimal(18,2)");
        builder.Property(m => m.IsActive).HasDefaultValue(true);
        builder.Property(m => m.IsPublic).HasDefaultValue(true);

        builder.Property(m => m.AiImageGenerationsUsed).HasDefaultValue(0);
        builder.Property(m => m.AiImageGenerationsPeriod).HasDefaultValue("").HasMaxLength(7);

        builder.HasMany(m => m.Clicks)
            .WithOne(c => c.Merchant)
            .HasForeignKey(c => c.MerchantId);

        builder.HasMany(m => m.Conversions)
            .WithOne(c => c.Merchant)
            .HasForeignKey(c => c.MerchantId);
    }
}
