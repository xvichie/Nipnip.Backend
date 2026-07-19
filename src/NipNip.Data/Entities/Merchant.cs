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
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Click> Clicks { get; set; } = [];
    public ICollection<Conversion> Conversions { get; set; } = [];
}
