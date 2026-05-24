namespace NipNip.Modules.Merchants.DTOs;

public record MerchantDashboardResponse(
    int TotalClicks,
    int TotalConversions,
    decimal TotalOwed,
    IReadOnlyList<TopCreatorEntry> TopCreators,
    DateTimeOffset? From,
    DateTimeOffset? To
);

public record TopCreatorEntry(
    Guid CreatorId,
    string CreatorName,
    string CreatorSlug,
    int Clicks,
    int Conversions,
    decimal TotalOwed
);
