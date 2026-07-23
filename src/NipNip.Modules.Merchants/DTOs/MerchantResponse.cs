namespace NipNip.Modules.Merchants.DTOs;

public record MerchantResponse(
    Guid Id,
    string Name,
    string Slug,
    string? LogoUrl,
    string? WebsiteUrl,
    string? InstagramHandle,
    string? Description,
    decimal CommissionPercent,
    decimal Balance,
    string ApiKey,
    string? NotificationEmail,
    bool IsActive,
    bool IsHighlighted,
    bool IsTest,
    bool IsPublic,
    bool IsProspect,
    bool IsApprovedForViewer,
    DateTimeOffset CreatedAt
);

public record MerchantAccessRequestResponse(
    Guid Id,
    Guid CreatorId,
    string CreatorName,
    string CreatorSlug,
    string? CreatorAvatarUrl,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RespondedAt
);

public record AddApprovedCreatorRequest(Guid CreatorId);

public record AiImageUsageResponse(int Used, int Limit, string Period);
