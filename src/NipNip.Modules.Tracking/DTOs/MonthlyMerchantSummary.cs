namespace NipNip.Modules.Tracking.DTOs;

public record MonthlyMerchantSummary(
    int Year,
    int Month,
    decimal TotalOwed,
    decimal TotalCommission,
    int Count
);
