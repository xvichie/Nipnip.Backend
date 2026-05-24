namespace NipNip.Modules.Merchants.DTOs;

public record RegisterMerchantRequest(
    string Name,
    string Slug,
    decimal CommissionPercent,
    string? WebsiteUrl,
    string? InstagramHandle,
    string? Description,
    string? LogoUrl
);
