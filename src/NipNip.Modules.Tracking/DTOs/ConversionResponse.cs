namespace NipNip.Modules.Tracking.DTOs;

public record ConversionResponse(
    Guid Id,
    Guid MerchantId,
    Guid CreatorId,
    Guid? ClickId,
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
