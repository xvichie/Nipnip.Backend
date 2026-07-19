using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class LinkTreeConfiguration : IEntityTypeConfiguration<LinkTree>
{
    public void Configure(EntityTypeBuilder<LinkTree> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.HasIndex(x => x.Slug).IsUnique();
    }
}
