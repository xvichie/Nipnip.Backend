namespace NipNip.Data.Entities;

public class StorePage
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Store Store { get; set; } = null!;
}
