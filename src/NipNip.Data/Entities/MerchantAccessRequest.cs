using NipNip.Data.Enums;

namespace NipNip.Data.Entities;

public class MerchantAccessRequest
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
    public Guid CreatorId { get; set; }
    public MerchantAccessRequestStatus Status { get; set; } = MerchantAccessRequestStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }

    public Merchant Merchant { get; set; } = null!;
    public Creator Creator { get; set; } = null!;
}
