namespace NipNip.Modules.Creators.DTOs;

public record CreatorDashboardResponse(
    int TotalClicks,
    int TotalConversions,
    decimal TotalEarned,
    IReadOnlyList<TopMerchantEntry> TopMerchants,
    DateTimeOffset? From,
    DateTimeOffset? To
);

public record TopMerchantEntry(
    Guid MerchantId,
    string MerchantName,
    string MerchantSlug,
    int Clicks,
    int Conversions,
    decimal CommissionEarned
);
