using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NipNip.Data.Entities;

namespace NipNip.Data.Configurations;

public class WebsiteInquiryConfiguration : IEntityTypeConfiguration<WebsiteInquiry>
{
    public void Configure(EntityTypeBuilder<WebsiteInquiry> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedOnAdd();

        builder.Property(i => i.Name).IsRequired();
        builder.Property(i => i.Message).IsRequired();

        builder.HasIndex(i => i.CreatedAt);

        // Postgres treats multiple NULLs as distinct under a unique index, so this only actually
        // constrains signed-in submitters (ClerkUserId set) to one row each — anonymous footer
        // submissions (ClerkUserId null) are unaffected.
        builder.HasIndex(i => i.ClerkUserId).IsUnique();
    }
}
