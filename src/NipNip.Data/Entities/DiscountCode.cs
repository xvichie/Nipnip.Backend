namespace NipNip.Data.Entities;

public class DiscountCode
{
    public Guid Id { get; set; }
    public Guid CreatorId { get; set; }
    public Creator Creator { get; set; } = null!;
    public Guid MerchantId { get; set; }
    public Merchant Merchant { get; set; } = null!;
    public string Code { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
