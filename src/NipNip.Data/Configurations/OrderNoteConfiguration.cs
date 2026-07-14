using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class OrderNoteConfiguration : IEntityTypeConfiguration<OrderNote>
{
    public void Configure(EntityTypeBuilder<OrderNote> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedOnAdd();

        builder.Property(n => n.Content).IsRequired();

        builder.HasOne(n => n.Order)
            .WithMany(o => o.Notes)
            .HasForeignKey(n => n.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
