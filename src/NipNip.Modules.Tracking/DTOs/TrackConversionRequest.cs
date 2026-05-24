namespace NipNip.Modules.Tracking.DTOs;

public record TrackConversionRequest(
    string Ref,
    string OrderId,
    decimal Amount,
    string? Currency
);
