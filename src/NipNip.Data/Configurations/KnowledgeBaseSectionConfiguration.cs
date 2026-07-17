using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class KnowledgeBaseSectionConfiguration : IEntityTypeConfiguration<KnowledgeBaseSection>
{
    public void Configure(EntityTypeBuilder<KnowledgeBaseSection> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();

        builder.Property(s => s.Title).IsRequired();
        builder.Property(s => s.Content).IsRequired();
        builder.Property(s => s.Embedding).HasColumnType("real[]");

        builder.HasIndex(s => s.StoreId);

        builder.HasOne(s => s.Store)
            .WithMany(store => store.KnowledgeBaseSections)
            .HasForeignKey(s => s.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
