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
    DateTimeOffset CreatedAt,
    /// <summary>When their Store was created — null if they haven't set one up yet. Only populated by the admin listing.</summary>
    DateTimeOffset? StoreCreatedAt = null
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

/// <summary>Provide exactly one of Url or Html — see FacebookImportService for why both exist.</summary>
public record ImportFacebookRequest(string? Url, string? Html);

/// <summary>ImageDataUri is a data: URI (e.g. "data:image/jpeg;base64,...") ready to hand to the frontend's existing image-upload flow, not a Facebook-hosted link.</summary>
public record ImportFacebookResponse(string? Name, string? Description, string? ImageDataUri);
