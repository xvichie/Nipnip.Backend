using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.ExternalUserId).IsRequired();
        builder.HasIndex(c => new { c.StoreId, c.ExternalUserId }).IsUnique();

        builder.HasOne(c => c.Store)
            .WithMany(s => s.Conversations)
            .HasForeignKey(c => c.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
