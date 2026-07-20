namespace NipNip.Data.Entities;

public class Store
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string ThemeId { get; set; } = "default";
    public string ThemeConfig { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public string? CustomDomain { get; set; }
    public DateTimeOffset? CustomDomainVerifiedAt { get; set; }
    public bool AffiliateEnabled { get; set; } = false;

    public string? FacebookPageId { get; set; }
    public string? FacebookPageName { get; set; }
    public string? FacebookPageAccessTokenEncrypted { get; set; }
    public DateTimeOffset? FacebookConnectedAt { get; set; }

    public bool AiAgentEnabledFacebook { get; set; } = false;
    public bool AiAgentEnabledInstagram { get; set; } = false;
    public string? AiAgentInstructions { get; set; }

    // Pickup location for courier delivery integrations (e.g. QuickShipper) — captured once
    // since every delivery order created needs a precise pickup address + coordinates.
    public string? PickupAddress { get; set; }
    public double? PickupLatitude { get; set; }
    public double? PickupLongitude { get; set; }
    public string? PickupContactName { get; set; }
    public string? PickupPhone { get; set; }

    // QuickShipper connection — Resource Owner Password Credentials grant, so we store the
    // merchant's own QuickShipper account credentials (encrypted) to re-authenticate whenever
    // the cached access token expires, rather than relying on an unconfirmed refresh-token flow.
    public string? QuickShipperUsernameEncrypted { get; set; }
    public string? QuickShipperPasswordEncrypted { get; set; }
    public string? QuickShipperAccessTokenEncrypted { get; set; }
    public DateTimeOffset? QuickShipperTokenExpiresAt { get; set; }
    public DateTimeOffset? QuickShipperConnectedAt { get; set; }

    // Flitt (hosted checkout payment gateway) — merchant_id isn't secret (just an account
    // number), but the payment secret key signs every request/callback and must stay
    // server-side only, so it's encrypted the same way as the QuickShipper credentials above.
    public string? FlittMerchantId { get; set; }
    public string? FlittSecretKeyEncrypted { get; set; }
    public DateTimeOffset? FlittConnectedAt { get; set; }

    // TikTok — Login Kit OAuth. Access tokens last 24h and refresh tokens 365 days, both
    // rotate on every refresh call, so both are cached encrypted and re-derived on demand
    // (mirrors the QuickShipper token-caching shape).
    public string? TikTokOpenId { get; set; }
    public string? TikTokDisplayName { get; set; }
    public string? TikTokAccessTokenEncrypted { get; set; }
    public string? TikTokRefreshTokenEncrypted { get; set; }
    public DateTimeOffset? TikTokTokenExpiresAt { get; set; }
    public DateTimeOffset? TikTokConnectedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Merchant Merchant { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
    public ICollection<Cart> Carts { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<Conversation> Conversations { get; set; } = [];
    public ICollection<KnowledgeBaseSection> KnowledgeBaseSections { get; set; } = [];
}
