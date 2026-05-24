namespace NipNip.Modules.Tracking.DTOs;

public record MonthlyCreatorSummary(
    int Year,
    int Month,
    decimal TotalEarnings,
    decimal TotalCommission,
    int Count
);
