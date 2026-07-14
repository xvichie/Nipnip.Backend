using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();

        builder.Property(s => s.Slug).IsRequired();
        builder.HasIndex(s => s.Slug).IsUnique();

        builder.Property(s => s.Name).IsRequired();
        builder.Property(s => s.ThemeId).IsRequired();

        builder.Property(s => s.ThemeConfig)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(s => s.IsActive).HasDefaultValue(true);

        builder.HasOne(s => s.Merchant)
            .WithOne()
            .HasForeignKey<Store>(s => s.MerchantId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(s => s.MerchantId).IsUnique();

        builder.HasMany(s => s.Categories)
            .WithOne(c => c.Store)
            .HasForeignKey(c => c.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Products)
            .WithOne(p => p.Store)
            .HasForeignKey(p => p.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Carts)
            .WithOne(c => c.Store)
            .HasForeignKey(c => c.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Orders)
            .WithOne(o => o.Store)
            .HasForeignKey(o => o.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
