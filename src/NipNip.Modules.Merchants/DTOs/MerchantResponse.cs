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
    bool IsApprovedForViewer,
    DateTimeOffset CreatedAt
);

public record ApprovedCreatorResponse(
    Guid CreatorId,
    string CreatorName,
    string CreatorSlug,
    string? CreatorAvatarUrl
);

public record AddApprovedCreatorRequest(Guid CreatorId);
