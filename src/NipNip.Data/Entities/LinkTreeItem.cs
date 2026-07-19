namespace NipNip.Data.Entities;

public class LinkTreeItem
{
    public Guid Id { get; set; }
    public Guid LinkTreeId { get; set; }
    public Guid MerchantId { get; set; }
    public string? Label { get; set; }
    public int Position { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public LinkTree LinkTree { get; set; } = null!;
    public Merchant Merchant { get; set; } = null!;
}
