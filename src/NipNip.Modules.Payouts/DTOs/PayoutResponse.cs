namespace NipNip.Modules.Payouts.DTOs;

public record PayoutResponse(
    Guid Id,
    Guid CreatorId,
    string CreatorName,
    string CreatorSlug,
    decimal RequestedAmount,
    decimal? AmountSent,
    string Currency,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt
);
