namespace NipNip.Data.Entities;

public class CartBundleItem
{
    public Guid Id { get; set; }
    public Guid CartId { get; set; }
    public Guid BundleId { get; set; }
    public int Quantity { get; set; }

    public Cart Cart { get; set; } = null!;
    public ProductBundle Bundle { get; set; } = null!;
}
