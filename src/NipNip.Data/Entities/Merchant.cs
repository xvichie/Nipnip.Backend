namespace NipNip.Data.Entities;

public class Merchant
{
    public Guid Id { get; set; }
    public string ClerkUserId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? InstagramHandle { get; set; }
    public string? Description { get; set; }
    public decimal CommissionPercent { get; set; }
    public decimal Balance { get; set; }
    public string ApiKey { get; set; } = "";
    public string? NotificationEmail { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsHighlighted { get; set; } = false;
    public bool IsTest { get; set; } = false;
    public bool IsPublic { get; set; } = true;

    // A sales-pitch demo store the admin builds out for a potential customer, before that
    // customer has a real Clerk account — see NipNip.Api's admin prospect endpoints. Never
    // shown in any public/creator-facing listing; the storefront itself is only reachable by
    // a signed-in admin via /preview/{slug}. ClerkUserId is a generated placeholder until
    // promotion, when an admin reassigns it to the real customer's Clerk user ID.
    public bool IsProspect { get; set; } = false;

    public DateTimeOffset CreatedAt { get; set; }

    // AI product-photo generation usage, reset whenever AiImageGenerationsPeriod no longer
    // matches the current "yyyy-MM" — see MerchantService.ConsumeAiImageGenerationAsync.
    public int AiImageGenerationsUsed { get; set; }
    public string AiImageGenerationsPeriod { get; set; } = "";

    public ICollection<Click> Clicks { get; set; } = [];
    public ICollection<Conversion> Conversions { get; set; } = [];
}
