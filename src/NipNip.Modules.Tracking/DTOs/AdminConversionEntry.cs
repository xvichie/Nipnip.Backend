namespace NipNip.Modules.Tracking.DTOs;

public record AdminConversionEntry(
    Guid Id,
    Guid MerchantId,
    string MerchantName,
    string MerchantSlug,
    Guid CreatorId,
    string CreatorName,
    string CreatorSlug,
    string OrderId,
    decimal OrderAmount,
    decimal CommissionAmount,
    decimal CreatorFeeAmount,
    decimal MerchantFeeAmount,
    decimal CreatorEarnings,
    string Currency,
    string Source,
    string Status,
    DateTimeOffset CreatedAt
);
