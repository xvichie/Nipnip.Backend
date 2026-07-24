namespace NipNip.Data.Entities;

public class ProductBundleItem
{
    public Guid Id { get; set; }
    public Guid BundleId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; } = 1;

    public ProductBundle Bundle { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
