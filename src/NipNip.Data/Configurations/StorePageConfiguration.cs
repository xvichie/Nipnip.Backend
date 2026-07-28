using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class StorePageConfiguration : IEntityTypeConfiguration<StorePage>
{
    public void Configure(EntityTypeBuilder<StorePage> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.Slug).IsRequired();
        builder.HasIndex(p => new { p.StoreId, p.Slug }).IsUnique();

        builder.HasOne(p => p.Store)
            .WithMany()
            .HasForeignKey(p => p.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
