namespace NipNip.Data.Entities;

// An "I want a website" lead — not tied to any Store/Merchant (unlike ContactMessage, which a
// shopper sends to a specific merchant's store). Submitted either anonymously from the public
// marketing site's footer form, or by a freshly signed-up Clerk user in place of the old
// self-serve creator onboarding form — see WebsiteInquiryService and app/onboarding/page.tsx.
// Read by the admin in the admin panel and followed up with manually.
public class WebsiteInquiry
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";

    // Only set when submitted from the post-signup onboarding form — null for the anonymous
    // footer form, which has no notion of "a store" yet.
    public string? StoreName { get; set; }

    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Message { get; set; } = "";

    // Set only when the submitter was signed in (the onboarding-replacement flow) — lets
    // WebsiteInquiryService.GetOwnAsync show them their own submission instead of the empty form
    // again on a later visit, and guards against the same signed-in user creating duplicates.
    // Null for the anonymous footer form.
    public string? ClerkUserId { get; set; }

    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
