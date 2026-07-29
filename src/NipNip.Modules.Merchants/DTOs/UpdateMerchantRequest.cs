namespace NipNip.Modules.Merchants.DTOs;

public record UpdateMerchantRequest(
    string? Name,
    string? WebsiteUrl,
    string? InstagramHandle,
    string? Description,
    string? LogoUrl,
    decimal? CommissionPercent,
    string? NotificationEmail,
    bool? IsPublic,
    /// <summary>Pass an empty string to clear it — a request property left null means "don't change".</summary>
    string? LogoBackgroundColor = null,
    /// <summary>Pass an empty string to clear it — a request property left null means "don't change".</summary>
    string? LogoBackgroundImageUrl = null
);
