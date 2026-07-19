namespace NipNip.Modules.Merchants.DTOs;

public record UpdateMerchantRequest(
    string? Name,
    string? WebsiteUrl,
    string? InstagramHandle,
    string? Description,
    string? LogoUrl,
    decimal? CommissionPercent,
    string? NotificationEmail,
    bool? IsPublic
);
