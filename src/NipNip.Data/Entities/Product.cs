namespace NipNip.Data.Entities;

public class Product
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public Guid? CategoryId { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
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
