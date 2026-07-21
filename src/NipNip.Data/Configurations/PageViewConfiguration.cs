using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class PageViewConfiguration : IEntityTypeConfiguration<PageView>
{
    public void Configure(EntityTypeBuilder<PageView> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedOnAdd();

        builder.Property(v => v.Path).IsRequired();
        builder.Property(v => v.VisitorId).IsRequired();

        // Every analytics query filters by store + date range, then often groups by visitor
        // (unique-visitor counts) or path (top pages) — index the pair plus the columns used
        // for those group-bys so aggregation stays fast as this table grows.
        builder.HasIndex(v => new { v.StoreId, v.CreatedAt });
        builder.HasIndex(v => new { v.StoreId, v.VisitorId });
    }
}
