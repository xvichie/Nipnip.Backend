namespace NipNip.Modules.Payouts.DTOs;

public record RequestPayoutRequest(
    decimal Amount,
    string Currency = "GEL"
);

public record MarkPayoutSentRequest(
    decimal AmountSent,
    string? Notes = null
);
