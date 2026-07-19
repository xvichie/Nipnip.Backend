namespace NipNip.Data.Entities;

public class LinkTree
{
    public Guid Id { get; set; }
    public Guid CreatorId { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public bool IsDefault { get; set; }
    public int Position { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Creator Creator { get; set; } = null!;
    public ICollection<LinkTreeItem> Items { get; set; } = [];
}
