using NipNip.Data.Enums;

namespace NipNip.Data.Entities;

public class Payout
{
    public Guid Id { get; set; }
    public Guid CreatorId { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal? AmountSent { get; set; }
    public string Currency { get; set; } = "GEL";
    public PayoutStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }

    public Creator Creator { get; set; } = null!;
}
