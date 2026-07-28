namespace NipNip.Data.Entities;

public class ProductOption
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    // At least one of these three must be set (enforced in ProductOptionService, not a DB
    // constraint) — same rule as Category/Product names.
    public string? NameKa { get; set; }
    public string? NameEn { get; set; }
    public string? NameRu { get; set; }

    public Product Product { get; set; } = null!;
    public ICollection<ProductOptionValue> Values { get; set; } = [];
}
