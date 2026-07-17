using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessage>
{
    public void Configure(EntityTypeBuilder<ConversationMessage> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd();

        builder.Property(m => m.Content).IsRequired();

        // Postgres allows multiple NULLs under a unique index, so messages with no
        // external id (e.g. debug-endpoint traffic) don't collide with each other.
        builder.HasIndex(m => m.ExternalMessageId).IsUnique();
    }
}
