using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd();

        builder.Property(m => m.Name).IsRequired();
        builder.Property(m => m.Message).IsRequired();

        builder.HasIndex(m => new { m.StoreId, m.CreatedAt });

        builder.HasOne(m => m.Store)
            .WithMany()
            .HasForeignKey(m => m.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
