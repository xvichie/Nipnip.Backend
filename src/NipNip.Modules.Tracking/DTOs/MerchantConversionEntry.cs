namespace NipNip.Modules.Tracking.DTOs;

public record MerchantConversionEntry(
    Guid Id,
    string CreatorName,
    string CreatorSlug,
    string OrderId,
    decimal OrderAmount,
    decimal CommissionAmount,
    decimal MerchantFeeAmount,
    decimal TotalOwed,
    string Currency,
    string Source,
    string Status,
    DateTimeOffset CreatedAt
);
