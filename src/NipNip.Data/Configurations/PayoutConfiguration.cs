using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    public void Configure(EntityTypeBuilder<Payout> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.RequestedAmount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.AmountSent).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Currency).HasDefaultValue("GEL");
    }
}
