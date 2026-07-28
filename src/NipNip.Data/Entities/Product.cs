namespace NipNip.Data.Entities;

public class Product
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public Guid? CategoryId { get; set; }
    // At least one of these three must be set (enforced in ProductService, not a DB
    // constraint) — a merchant can enter just one language and add the others later.
    public string? NameKa { get; set; }
    public string? NameEn { get; set; }
    public string? NameRu { get; set; }
    public string Slug { get; set; } = "";
    public string? DescriptionKa { get; set; }
    public string? DescriptionEn { get; set; }
    public string? DescriptionRu { get; set; }
    public string? VideoUrl { get; set; }
    public decimal BasePrice { get; set; }
    public decimal? SalePrice { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    public Store Store { get; set; } = null!;
    public Category? Category { get; set; }
    public ICollection<ProductImage> Images { get; set; } = [];
    public ICollection<ProductOption> Options { get; set; } = [];
    public ICollection<ProductVariant> Variants { get; set; } = [];
    public ICollection<ProductCollection> ProductCollections { get; set; } = [];
}
