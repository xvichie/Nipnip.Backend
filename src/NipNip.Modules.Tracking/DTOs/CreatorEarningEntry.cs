namespace NipNip.Modules.Tracking.DTOs;

public record CreatorEarningEntry(
    Guid Id,
    string MerchantName,
    string MerchantSlug,
    string OrderId,
    decimal OrderAmount,
    decimal CommissionAmount,
    decimal CreatorFeeAmount,
    decimal CreatorEarnings,
    string Currency,
    string Status,
    DateTimeOffset CreatedAt
);
