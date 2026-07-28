namespace NipNip.Data.Entities;

public class StorePage
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    // At least one of these three must be set (enforced in StorePageService, not a DB
    // constraint) — same rule as Category/Product names, applied to both Title and Content
    // since a page with nothing to show in any language has no reason to exist.
    public string? TitleKa { get; set; }
    public string? TitleEn { get; set; }
    public string? TitleRu { get; set; }
    public string Slug { get; set; } = "";
    public string? ContentKa { get; set; }
    public string? ContentEn { get; set; }
    public string? ContentRu { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Store Store { get; set; } = null!;
}
