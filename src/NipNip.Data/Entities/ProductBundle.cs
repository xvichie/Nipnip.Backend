namespace NipNip.Data.Entities;

// A merchant-curated set of products sold together at a fixed combined price (e.g. "Starter
// Kit"). Deliberately simpler than Product: no options/variants — each item is just a product
// + quantity, since kit contents are fixed by the merchant rather than picked by the customer.
public class ProductBundle
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }

    // At least one of these three must be set (enforced in ProductBundleService, not a DB
    // constraint) — same rule as Category/Product/Collection names.
    public string? NameKa { get; set; }
    public string? NameEn { get; set; }
    public string? NameRu { get; set; }

    public string Slug { get; set; } = "";
    public decimal BundlePrice { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    public Store Store { get; set; } = null!;
    public ICollection<ProductBundleItem> Items { get; set; } = [];
}
