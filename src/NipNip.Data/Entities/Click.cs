namespace NipNip.Data.Entities;

public class Click
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
    public Guid CreatorId { get; set; }
    public string RefCode { get; set; } = "";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset ClickedAt { get; set; }

    public Merchant Merchant { get; set; } = null!;
    public Creator Creator { get; set; } = null!;
}
