namespace NipNip.Data.Entities;

public class MerchantApprovedCreator
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
    public Guid CreatorId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Merchant Merchant { get; set; } = null!;
    public Creator Creator { get; set; } = null!;
}
