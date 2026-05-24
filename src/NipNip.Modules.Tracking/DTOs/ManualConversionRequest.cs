namespace NipNip.Modules.Tracking.DTOs;

public record ManualConversionRequest(
    string CreatorSlug,
    decimal OrderAmount,
    string? OrderId,
    string? Currency
);
