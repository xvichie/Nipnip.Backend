namespace NipNip.Data.Entities;

public class Creator
{
    public Guid Id { get; set; }
    public string ClerkUserId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public string? InstagramHandle { get; set; }
    public int? InstagramFollowers { get; set; }
    public string? TiktokHandle { get; set; }
    public int? TiktokFollowers { get; set; }
    public string? YoutubeHandle { get; set; }
    public int? YoutubeFollowers { get; set; }
    public string? FacebookHandle { get; set; }
    public int? FacebookFollowers { get; set; }
    public string? XHandle { get; set; }
    public int? XFollowers { get; set; }
    public string? LinkedinHandle { get; set; }
    public int? LinkedinFollowers { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsHighlighted { get; set; } = false;
    public bool IsTest { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Click> Clicks { get; set; } = [];
    public ICollection<Conversion> Conversions { get; set; } = [];
    public ICollection<Payout> Payouts { get; set; } = [];
}
