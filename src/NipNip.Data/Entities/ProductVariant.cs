namespace NipNip.Data.Entities;

public class ProductVariant
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = "";
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }

    /// <summary>Null means unlimited stock.</summary>
    public int? Stock { get; set; }

    public Product Product { get; set; } = null!;
    public ICollection<ProductVariantOptionValue> OptionValues { get; set; } = [];
}
