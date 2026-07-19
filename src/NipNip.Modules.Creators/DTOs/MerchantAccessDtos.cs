namespace NipNip.Modules.Creators.DTOs;

public record CreatorAccessRequestResponse(
    Guid Id,
    Guid MerchantId,
    string MerchantName,
    string MerchantSlug,
    string? MerchantLogoUrl,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RespondedAt
);

public record RequestMerchantAccessRequest(Guid MerchantId);
