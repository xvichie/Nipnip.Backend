using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class LinkTreeItemConfiguration : IEntityTypeConfiguration<LinkTreeItem>
{
    public void Configure(EntityTypeBuilder<LinkTreeItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.HasIndex(x => new { x.CreatorId, x.MerchantId }).IsUnique();
    }
}
